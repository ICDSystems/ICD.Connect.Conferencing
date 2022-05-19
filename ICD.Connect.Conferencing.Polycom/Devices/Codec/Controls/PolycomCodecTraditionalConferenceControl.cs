using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.IncomingCalls;
using ICD.Connect.Conferencing.Participants.Enums;
using ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.AutoAnswer;
using ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Dial;
using ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Mute;
using ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.System;
using ICD.Connect.Conferencing.Utils;
using eDialProtocol = ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Dial.eDialProtocol;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec.Controls
{
	public sealed class PolycomCodecTraditionalConferenceControl : AbstractConferenceDeviceControl<PolycomGroupSeriesDevice, ThinConference>
	{
		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallAdded;
		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallRemoved;
		public override event EventHandler<ConferenceEventArgs> OnConferenceAdded;
		public override event EventHandler<ConferenceEventArgs> OnConferenceRemoved;

		private readonly BiDictionary<int, ThinConference> m_Conferences;
		private readonly BiDictionary<int, TraditionalIncomingCall> m_IncomingCalls;
		private readonly SafeCriticalSection m_ConferencesSection;

		private readonly DialComponent m_DialComponent;
		private readonly AutoAnswerComponent m_AutoAnswerComponent;
		private readonly MuteComponent m_MuteComponent;
		private readonly SystemSettingComponent m_SystemSettingComponent;

		private bool m_RequestedPrivacyMute;
		private bool m_RequestedHold;

		#region Properties

		/// <summary>
		/// Gets the type of conference this dialer supports.
		/// </summary>
		public override eCallType Supports { get { return eCallType.Video | eCallType.Audio; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public PolycomCodecTraditionalConferenceControl(PolycomGroupSeriesDevice parent, int id)
			: base(parent, id)
		{
			m_Conferences = new BiDictionary<int, ThinConference>();
			m_IncomingCalls = new BiDictionary<int, TraditionalIncomingCall>();
			m_ConferencesSection = new SafeCriticalSection();

			m_DialComponent = parent.Components.GetComponent<DialComponent>();
			m_AutoAnswerComponent = parent.Components.GetComponent<AutoAnswerComponent>();
			m_MuteComponent = parent.Components.GetComponent<MuteComponent>();
			m_SystemSettingComponent = parent.Components.GetComponent<SystemSettingComponent>();

			SupportedConferenceControlFeatures =
				eConferenceControlFeatures.AutoAnswer |
				eConferenceControlFeatures.DoNotDisturb |
				eConferenceControlFeatures.PrivacyMute |
				eConferenceControlFeatures.CameraMute |
				eConferenceControlFeatures.Dtmf |
				eConferenceControlFeatures.CanDial |
				eConferenceControlFeatures.CanEnd;

			Subscribe(m_DialComponent);
			Subscribe(m_AutoAnswerComponent);
			Subscribe(m_MuteComponent);
			Subscribe(m_SystemSettingComponent);
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnIncomingCallAdded = null;
			OnIncomingCallRemoved = null;

			base.DisposeFinal(disposing);

			Unsubscribe(m_DialComponent);
			Unsubscribe(m_AutoAnswerComponent);
			Unsubscribe(m_MuteComponent);
			Unsubscribe(m_SystemSettingComponent);

			RemoveSources();
		}

		#region Methods

		public override IEnumerable<ThinConference> GetConferences()
		{
			return m_ConferencesSection.Execute(() => m_Conferences.Values.ToArray(m_Conferences.Count));
		}

		/// <summary>
		/// Returns the level of support the dialer has for the given booking.
		/// </summary>
		/// <param name="dialContext"></param>
		/// <returns></returns>
		public override eDialContextSupport CanDial(IDialContext dialContext)
		{
			if (string.IsNullOrEmpty(dialContext.DialString))
				return eDialContextSupport.Unsupported;

			if (dialContext.Protocol == DialContexts.eDialProtocol.Sip && SipUtils.IsValidSipUri(dialContext.DialString))
				return eDialContextSupport.Supported;

			if (dialContext.Protocol == DialContexts.eDialProtocol.Pstn)
				return eDialContextSupport.Supported;

			if (dialContext.Protocol == DialContexts.eDialProtocol.Unknown)
				return eDialContextSupport.Unknown;

			return eDialContextSupport.Unsupported;
		}

		/// <summary>
		/// Dials the given booking.
		/// </summary>
		/// <param name="dialContext"></param>
		public override void Dial(IDialContext dialContext)
		{
			if (dialContext.CallType == eCallType.Video || dialContext.CallType == eCallType.Unknown)
				m_DialComponent.DialAuto(dialContext.DialString);

			else if (dialContext.CallType == eCallType.Audio)
				m_DialComponent.DialPhone(eDialProtocol.Auto, dialContext.DialString);
		}

		/// <summary>
		/// Sets the do-not-disturb enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public override void SetDoNotDisturb(bool enabled)
		{
			// Don't leave Auto-Answer mode
			if (!enabled && m_AutoAnswerComponent.AutoAnswer == eAutoAnswer.Yes)
				return;

			m_AutoAnswerComponent.SetAutoAnswer(enabled ? eAutoAnswer.DoNotDisturb : eAutoAnswer.No);
		}

		/// <summary>
		/// Sets the auto-answer enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public override void SetAutoAnswer(bool enabled)
		{
			// Don't leave Do-Not-Disturb mode
			if (!enabled && m_AutoAnswerComponent.AutoAnswer == eAutoAnswer.DoNotDisturb)
				return;

			m_AutoAnswerComponent.SetAutoAnswer(enabled ? eAutoAnswer.Yes : eAutoAnswer.No);
		}

		/// <summary>
		/// Sets the privacy mute enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public override void SetPrivacyMute(bool enabled)
		{
			m_RequestedPrivacyMute = enabled;

			UpdateMute();
		}

		/// <summary>
		/// Sets the camera mute state.
		/// </summary>
		/// <param name="mute"></param>
		public override void SetCameraMute(bool mute)
		{
			m_MuteComponent.MuteVideo(mute);
		}

		public override void StartPersonalMeeting()
		{
			throw new NotSupportedException();
		}

		public override void EnableCallLock(bool enabled)
		{
			throw new NotSupportedException();
		}

		#endregion

		/// <summary>
		/// Enforces the near/far/video mute states on the device to match requested values.
		/// </summary>
		private void UpdateMute()
		{
			bool videoMute = m_RequestedHold;
			bool nearMute = m_RequestedHold || m_RequestedPrivacyMute;

			m_MuteComponent.MuteVideo(videoMute);
			m_MuteComponent.MuteNear(nearMute);
		}

		#region Dial Component Callbacks

		/// <summary>
		/// Subscribe to the component events.
		/// </summary>
		/// <param name="dialComponent"></param>
		private void Subscribe(DialComponent dialComponent)
		{
			dialComponent.OnCallStatesChanged += DialComponentOnCallStatesChanged;
		}

		/// <summary>
		/// Unsubscribe from the component events.
		/// </summary>
		/// <param name="dialComponent"></param>
		private void Unsubscribe(DialComponent dialComponent)
		{
			dialComponent.OnCallStatesChanged -= DialComponentOnCallStatesChanged;
		}

		/// <summary>
		/// Called when a call state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void DialComponentOnCallStatesChanged(object sender, EventArgs eventArgs)
		{
			Dictionary<int, CallStatus> statuses = m_DialComponent.GetCallStatuses().ToDictionary(s => s.CallId);

			m_ConferencesSection.Enter();

			try
			{
				// Clear out sources that are no longer online
				IcdHashSet<int> remove =
					m_IncomingCalls.Keys
					               .Concat(m_Conferences.Keys)
					               .Where(id => !statuses.ContainsKey(id))
					               .ToIcdHashSet();

				RemoveIncomingCalls(remove);
				RemoveSources(remove);

				// Update new/existing sources
				foreach (KeyValuePair<int, CallStatus> kvp in statuses)
				{
					if (m_IncomingCalls.ContainsKey(kvp.Key))
						UpdateIncomingCall(kvp.Value);

					if (m_Conferences.ContainsKey(kvp.Key))
						UpdateConferenceFromCallStatus(kvp.Value);

					switch (kvp.Value.ConnectionState)
					{
						case eConnectionState.Unknown:
							break;

						// Ignore inactive state, it's muddled around disconnected/disconnecting
						case eConnectionState.Inactive:
							break;

						// incoming calls
						case eConnectionState.Ringing:
							CreateIncomingCall(kvp.Value);
							break;

						// current calls
						case eConnectionState.Opened:
						case eConnectionState.Connecting:
						case eConnectionState.Connected:
						case eConnectionState.Disconnecting:
							CreateConfereneFromCallStatus(kvp.Value);
							break;

						case eConnectionState.Disconnected:
							RemoveIncomingCall(kvp.Key);
							RemoveConference(kvp.Key);
							break;
					}
				}
			}
			finally
			{
				m_ConferencesSection.Leave();
			}
		}

		/// <summary>
		/// Removes all of the current sources.
		/// </summary>
		private void RemoveSources()
		{
			m_ConferencesSection.Enter();

			try
			{
				RemoveSources(m_Conferences.Keys.ToArray(m_Conferences.Count));
			}
			finally
			{
				m_ConferencesSection.Leave();
			}
		}

		/// <summary>
		/// Removes all of the sources with the given ids.
		/// </summary>
		private void RemoveSources(IEnumerable<int> ids)
		{
			if (ids == null)
				throw new ArgumentNullException("ids");

			foreach (int id in ids)
				RemoveConference(id);
		}

		/// <summary>
		/// Removes the source with the given id.
		/// </summary>
		/// <param name="id"></param>
		private void RemoveConference(int id)
		{
			ThinConference conference;

			m_ConferencesSection.Enter();

			try
			{
				if (!m_Conferences.TryGetValue(id, out conference))
					return;

				conference.Status = eConferenceStatus.Disconnected;

				Unsubscribe(conference);

				m_Conferences.RemoveKey(id);

				// Leave hold state when out of calls
				if (m_Conferences.Count == 0)
				{
					m_RequestedPrivacyMute = false;
					UpdateMute();
				}
			}
			finally
			{
				m_ConferencesSection.Leave();
			}

			OnConferenceRemoved.Raise(this, conference);
		}

		/// <summary>
		/// Creates a source for the given call status.
		/// </summary>
		/// <param name="callStatus"></param>
		private void CreateConfereneFromCallStatus(CallStatus callStatus)
		{
			if (callStatus == null)
				throw new ArgumentNullException("callStatus");

			ThinConference conference;

			m_ConferencesSection.Enter();

			try
			{
				if (m_Conferences.ContainsKey(callStatus.CallId))
					return;

				conference = new ThinConference();
				m_Conferences.Add(callStatus.CallId, conference);

				UpdateConferenceFromCallStatus(callStatus);
				Subscribe(conference);
			}
			finally
			{
				m_ConferencesSection.Leave();
			}

			OnConferenceAdded.Raise(this, conference);
		}

		/// <summary>
		/// Updates the source matching the given call status.
		/// </summary>
		/// <param name="callStatus"></param>
		private void UpdateConferenceFromCallStatus(CallStatus callStatus)
		{
			if (callStatus == null)
				throw new ArgumentNullException("callStatus");

			ThinConference conference = m_ConferencesSection.Execute(() => m_Conferences.GetDefault(callStatus.CallId, null));
			if (conference == null)
				return;

			// Prevents overriding a resolved name with a number when the call disconnects
			if (callStatus.FarSiteName != callStatus.FarSiteNumber)
				conference.Name = callStatus.FarSiteName;

			conference.Number = callStatus.FarSiteNumber;

			bool? outgoing = callStatus.Outgoing;
			if (outgoing == null)
				conference.Direction = eCallDirection.Undefined;
			else
				conference.Direction = (bool)outgoing ? eCallDirection.Outgoing : eCallDirection.Incoming;

			conference.Status = GetStatus(callStatus.ConnectionState);
			conference.CallType = callStatus.VideoCall ? eCallType.Video : eCallType.Audio;

			if (conference.IsActive())
				conference.StartTime = conference.StartTime ?? IcdEnvironment.GetUtcTime();
			else if (conference.StartTime != null)
				conference.EndTime = conference.EndTime ?? IcdEnvironment.GetUtcTime();
		}

		/// <summary>
		/// Gets the source status based on the given connection state.
		/// </summary>
		/// <param name="connectionState"></param>
		/// <returns></returns>
		private static eConferenceStatus GetStatus(eConnectionState connectionState)
		{
			switch (connectionState)
			{
				case eConnectionState.Unknown:
					return eConferenceStatus.Undefined;
				case eConnectionState.Opened:
				case eConnectionState.Ringing:
				case eConnectionState.Connecting:
					return eConferenceStatus.Connecting;
				case eConnectionState.Connected:
					return eConferenceStatus.Connected;
				case eConnectionState.Inactive:
				case eConnectionState.Disconnecting:
					return eConferenceStatus.Disconnecting;
				case eConnectionState.Disconnected:
					return eConferenceStatus.Disconnected;
				default:
					throw new ArgumentOutOfRangeException("connectionState");
			}
		}

		/// <summary>
		/// Removes all of the sources with the given ids.
		/// </summary>
		private void RemoveIncomingCalls(IEnumerable<int> ids)
		{
			if (ids == null)
				throw new ArgumentNullException("ids");

			foreach (int id in ids)
				RemoveIncomingCall(id);
		}

		/// <summary>
		/// Removes the source with the given id.
		/// </summary>
		/// <param name="id"></param>
		private void RemoveIncomingCall(int id)
		{
			TraditionalIncomingCall call;

			m_ConferencesSection.Enter();

			try
			{
				if (!m_IncomingCalls.TryGetValue(id, out call))
					return;

				Unsubscribe(call);

				m_IncomingCalls.RemoveKey(id);
			}
			finally
			{
				m_ConferencesSection.Leave();
			}

			OnIncomingCallRemoved.Raise(this, new GenericEventArgs<IIncomingCall>(call));
		}

		/// <summary>
		/// Creates a source for the given call status.
		/// </summary>
		/// <param name="callStatus"></param>
		private void CreateIncomingCall(CallStatus callStatus)
		{
			if (callStatus == null)
				throw new ArgumentNullException("callStatus");

			TraditionalIncomingCall call;

			m_ConferencesSection.Enter();

			try
			{
				if (m_IncomingCalls.ContainsKey(callStatus.CallId))
					return;

				call = new TraditionalIncomingCall(eCallType.Video) { AnswerState = eCallAnswerState.Unanswered };
				m_IncomingCalls.Add(callStatus.CallId, call);

				UpdateIncomingCall(callStatus);
				Subscribe(call);
			}
			finally
			{
				m_ConferencesSection.Leave();
			}

			OnIncomingCallAdded.Raise(this, new GenericEventArgs<IIncomingCall>(call));
		}

		/// <summary>
		/// Updates the source matching the given call status.
		/// </summary>
		/// <param name="callStatus"></param>
		private void UpdateIncomingCall(CallStatus callStatus)
		{
			if (callStatus == null)
				throw new ArgumentNullException("callStatus");

			TraditionalIncomingCall call = m_ConferencesSection.Execute(() => m_IncomingCalls.GetDefault(callStatus.CallId, null));
			if (call == null)
				return;

			// Prevents overriding a resolved name with a number when the call disconnects
			if (callStatus.FarSiteName != callStatus.FarSiteNumber)
				call.Name = callStatus.FarSiteName;

			call.Number = callStatus.FarSiteNumber;

			if (callStatus.ConnectionState == eConnectionState.Connected)
			{
				call.AnswerState = eCallAnswerState.Answered;
				RemoveIncomingCall(callStatus.CallId);
			}
			else if (callStatus.ConnectionState == eConnectionState.Disconnected)
			{
				call.AnswerState = eCallAnswerState.Ignored;
				RemoveIncomingCall(callStatus.CallId);
			}
		}

		#endregion

		#region Conference Callbacks

		/// <summary>
		/// Subscribe to the source events.
		/// </summary>
		/// <param name="conference"></param>
		private void Subscribe(ThinConference conference)
		{
			
			conference.HoldCallback = HoldCallback;
			conference.ResumeCallback = ResumeCallback;
			conference.SendDtmfCallback = SendDtmfCallback;
			conference.LeaveConferenceCallback = HangupCallback;
		}

		/// <summary>
		/// Unsubscribe from the source events.
		/// </summary>
		/// <param name="conference"></param>
		private void Unsubscribe(ThinConference conference)
		{
			conference.HoldCallback = null;
			conference.ResumeCallback = null;
			conference.SendDtmfCallback = null;
			conference.LeaveConferenceCallback = null;
		}

		private void HangupCallback(ThinConference sender)
		{
			int id = GetIdForConference(sender);

			m_DialComponent.HangupVideo(id);
		}

		private void SendDtmfCallback(ThinConference sender, string data)
		{
			data.ForEach(c => m_DialComponent.Gendial(c));
		}

		private void ResumeCallback(ThinConference sender)
		{
			m_RequestedHold = false;

			UpdateMute();
		}

		private void HoldCallback(ThinConference sender)
		{
			m_RequestedHold = true;

			UpdateMute();
		}

		private int GetIdForConference(ThinConference source)
		{
			return m_ConferencesSection.Execute(() => m_Conferences.GetKey(source));
		}

		#endregion

		#region Incoming Call Callbacks

		private void Subscribe(TraditionalIncomingCall call)
		{
			call.AnswerCallback = AnswerCallback;
			call.RejectCallback = RejectCallback;
		}

		private void Unsubscribe(TraditionalIncomingCall call)
		{
			call.AnswerCallback = null;
			call.RejectCallback = null;
		}

		private void RejectCallback(IIncomingCall sender)
		{
			int id = GetIdForIncomingCall(sender as TraditionalIncomingCall);

			m_DialComponent.HangupVideo(id);
		}

		private void AnswerCallback(IIncomingCall sender)
		{
			m_DialComponent.AnswerVideo();
		}

		private int GetIdForIncomingCall(TraditionalIncomingCall call)
		{
			return m_ConferencesSection.Execute(() => m_IncomingCalls.GetKey(call));
		}

		#endregion

		#region AutoAnswer Component Callbacks

		/// <summary>
		/// Subscribe to the component events.
		/// </summary>
		/// <param name="autoAnswerComponent"></param>
		private void Subscribe(AutoAnswerComponent autoAnswerComponent)
		{
			autoAnswerComponent.OnAutoAnswerChanged += AutoAnswerComponentOnAutoAnswerChanged;
		}

		/// <summary>
		/// Unsubscribe from the component events.
		/// </summary>
		/// <param name="autoAnswerComponent"></param>
		private void Unsubscribe(AutoAnswerComponent autoAnswerComponent)
		{
			autoAnswerComponent.OnAutoAnswerChanged -= AutoAnswerComponentOnAutoAnswerChanged;
		}

		/// <summary>
		/// Called when the autoanswer mode changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void AutoAnswerComponentOnAutoAnswerChanged(object sender, PolycomAutoAnswerEventArgs eventArgs)
		{
			AutoAnswer = m_AutoAnswerComponent.AutoAnswer == eAutoAnswer.Yes;
			DoNotDisturb = m_AutoAnswerComponent.AutoAnswer == eAutoAnswer.DoNotDisturb;

			UpdateMute();
		}

		#endregion

		#region Mute Component Callbacks

		/// <summary>
		/// Subscribe to the component events.
		/// </summary>
		/// <param name="muteComponent"></param>
		private void Subscribe(MuteComponent muteComponent)
		{
			muteComponent.OnMutedNearChanged += MuteComponentOnMutedNearChanged;
			muteComponent.OnVideoMutedChanged += MuteComponentOnVideoMutedChanged;
		}

		/// <summary>
		/// Unsubscribe from the component events.
		/// </summary>
		/// <param name="muteComponent"></param>
		private void Unsubscribe(MuteComponent muteComponent)
		{
			muteComponent.OnMutedNearChanged -= MuteComponentOnMutedNearChanged;
			muteComponent.OnVideoMutedChanged -= MuteComponentOnVideoMutedChanged;
		}

		/// <summary>
		/// Called when the video mute state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="boolEventArgs"></param>
		private void MuteComponentOnVideoMutedChanged(object sender, BoolEventArgs boolEventArgs)
		{
			UpdateMute();
		}

		/// <summary>
		/// Called when the near privacy mute state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="boolEventArgs"></param>
		private void MuteComponentOnMutedNearChanged(object sender, BoolEventArgs boolEventArgs)
		{
			PrivacyMuted = m_MuteComponent.MutedNear;

			UpdateMute();
		}

		#endregion

		#region System Setting Component Callbacks

		/// <summary>
		/// Subscribe to the system setting component events.
		/// </summary>
		/// <param name="systemSettingComponent"></param>
		private void Subscribe(SystemSettingComponent systemSettingComponent)
		{
			systemSettingComponent.OnSipAccountNameChanged += SystemSettingComponentOnSipAccountNameChanged;
		}

		/// <summary>
		/// Unsubscribe from the system setting component events.
		/// </summary>
		/// <param name="systemSettingComponent"></param>
		private void Unsubscribe(SystemSettingComponent systemSettingComponent)
		{
			systemSettingComponent.OnSipAccountNameChanged -= SystemSettingComponentOnSipAccountNameChanged;
		}

		/// <summary>
		/// Called when the sip account name changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="stringEventArgs"></param>
		private void SystemSettingComponentOnSipAccountNameChanged(object sender, StringEventArgs stringEventArgs)
		{
			CallInInfo =
				new DialContext
				{
					Protocol = DialContexts.eDialProtocol.Sip,
					CallType = eCallType.Audio | eCallType.Video,
					DialString = m_SystemSettingComponent.SipAccountName
				};
		}

		#endregion
	}
}
