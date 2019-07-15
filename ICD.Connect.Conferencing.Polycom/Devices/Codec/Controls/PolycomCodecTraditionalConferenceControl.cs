using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;
using ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.AutoAnswer;
using ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Dial;
using ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Mute;
using ICD.Connect.Conferencing.Utils;
using eDialProtocol = ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Dial.eDialProtocol;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec.Controls
{
	public sealed class PolycomCodecTraditionalConferenceControl : AbstractTraditionalConferenceDeviceControl<PolycomGroupSeriesDevice>
	{
		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallAdded;
		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallRemoved;

		private readonly BiDictionary<int, ThinTraditionalParticipant> m_Participants;
		private readonly BiDictionary<int, ThinIncomingCall> m_IncomingCalls;
		private readonly SafeCriticalSection m_ParticipantsSection;

		private readonly DialComponent m_DialComponent;
		private readonly AutoAnswerComponent m_AutoAnswerComponent;
		private readonly MuteComponent m_MuteComponent;

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
			m_Participants = new BiDictionary<int, ThinTraditionalParticipant>();
			m_IncomingCalls = new BiDictionary<int, ThinIncomingCall>();
			m_ParticipantsSection = new SafeCriticalSection();

			m_DialComponent = parent.Components.GetComponent<DialComponent>();
			m_AutoAnswerComponent = parent.Components.GetComponent<AutoAnswerComponent>();
			m_MuteComponent = parent.Components.GetComponent<MuteComponent>();

			Subscribe(m_DialComponent);
			Subscribe(m_AutoAnswerComponent);
			Subscribe(m_MuteComponent);
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

			RemoveSources();
		}

		#region Methods

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

			m_ParticipantsSection.Enter();

			try
			{
				// Clear out sources that are no longer online
				IcdHashSet<int> remove =
					m_IncomingCalls.Where(kvp => !statuses.ContainsKey(kvp.Key))
							 .Select(kvp => kvp.Key)
							 .ToIcdHashSet();

				RemoveIncomingCalls(remove);
				RemoveSources(remove);

				// Update new/existing sources
				foreach (KeyValuePair<int, CallStatus> kvp in statuses)
				{
					if (m_IncomingCalls.ContainsKey(kvp.Key))
						UpdateIncomingCall(kvp.Value);

					if (m_Participants.ContainsKey(kvp.Key))
						UpdateSource(kvp.Value);

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
							CreateSource(kvp.Value);
							break;

						case eConnectionState.Disconnected:
							RemoveIncomingCall(kvp.Key);
							RemoveSource(kvp.Key);
							break;
					}
				}
			}
			finally
			{
				m_ParticipantsSection.Leave();
			}
		}

		/// <summary>
		/// Removes all of the current sources.
		/// </summary>
		private void RemoveSources()
		{
			m_ParticipantsSection.Enter();

			try
			{
				RemoveSources(m_Participants.Keys.ToArray(m_Participants.Count));
			}
			finally
			{
				m_ParticipantsSection.Leave();
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
				RemoveSource(id);
		}

		/// <summary>
		/// Removes the source with the given id.
		/// </summary>
		/// <param name="id"></param>
		private void RemoveSource(int id)
		{
			ThinTraditionalParticipant source;

			m_ParticipantsSection.Enter();

			try
			{
				if (!m_Participants.ContainsKey(id))
					return;

				source = m_Participants.GetValue(id);
				source.Status = eParticipantStatus.Disconnected;

				Unsubscribe(source);

				m_Participants.RemoveKey(id);

				// Leave hold state when out of calls
				if (m_Participants.Count == 0)
				{
					m_RequestedPrivacyMute = false;
					UpdateMute();
				}
			}
			finally
			{
				m_ParticipantsSection.Leave();
			}

			RemoveParticipant(source);
		}

		/// <summary>
		/// Creates a source for the given call status.
		/// </summary>
		/// <param name="callStatus"></param>
		private void CreateSource(CallStatus callStatus)
		{
			if (callStatus == null)
				throw new ArgumentNullException("callStatus");

			ThinTraditionalParticipant source;

			m_ParticipantsSection.Enter();

			try
			{
				if (m_Participants.ContainsKey(callStatus.CallId))
					return;

				source = new ThinTraditionalParticipant();
				m_Participants.Add(callStatus.CallId, source);

				UpdateSource(callStatus);
				Subscribe(source);
			}
			finally
			{
				m_ParticipantsSection.Leave();
			}

			AddParticipant(source);
		}

		/// <summary>
		/// Updates the source matching the given call status.
		/// </summary>
		/// <param name="callStatus"></param>
		private void UpdateSource(CallStatus callStatus)
		{
			if (callStatus == null)
				throw new ArgumentNullException("callStatus");

			ThinTraditionalParticipant source = m_ParticipantsSection.Execute(() => m_Participants.GetDefault(callStatus.CallId, null));
			if (source == null)
				return;

			// Prevents overriding a resolved name with a number when the call disconnects
			if (callStatus.FarSiteName != callStatus.FarSiteNumber)
				source.Name = callStatus.FarSiteName;

			source.Number = callStatus.FarSiteNumber;

			bool? outgoing = callStatus.Outgoing;
			if (outgoing == null)
				source.Direction = eCallDirection.Undefined;
			else
				source.Direction = (bool)outgoing ? eCallDirection.Outgoing : eCallDirection.Incoming;

			source.Status = GetStatus(callStatus.ConnectionState);
			source.SourceType = callStatus.VideoCall ? eCallType.Video : eCallType.Audio;

			if (source.GetIsOnline())
				source.Start = source.Start ?? IcdEnvironment.GetLocalTime();
			else if (source.Start != null)
				source.End = source.End ?? IcdEnvironment.GetLocalTime();
		}

		/// <summary>
		/// Gets the source status based on the given connection state.
		/// </summary>
		/// <param name="connectionState"></param>
		/// <returns></returns>
		private static eParticipantStatus GetStatus(eConnectionState connectionState)
		{
			switch (connectionState)
			{
				case eConnectionState.Unknown:
					return eParticipantStatus.Undefined;
				case eConnectionState.Opened:
				case eConnectionState.Ringing:
					return eParticipantStatus.Ringing;
				case eConnectionState.Connecting:
					return eParticipantStatus.Connecting;
				case eConnectionState.Connected:
					return eParticipantStatus.Connected;
				case eConnectionState.Inactive:
				case eConnectionState.Disconnecting:
					return eParticipantStatus.Disconnecting;
				case eConnectionState.Disconnected:
					return eParticipantStatus.Disconnected;
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
			ThinIncomingCall call;

			m_ParticipantsSection.Enter();

			try
			{
				if (!m_Participants.ContainsKey(id))
					return;

				call = m_IncomingCalls.GetValue(id);

				Unsubscribe(call);

				m_IncomingCalls.RemoveKey(id);
			}
			finally
			{
				m_ParticipantsSection.Leave();
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

			ThinIncomingCall call;

			m_ParticipantsSection.Enter();

			try
			{
				if (m_IncomingCalls.ContainsKey(callStatus.CallId))
					return;

				call = new ThinIncomingCall { AnswerState = eCallAnswerState.Unanswered };
				m_IncomingCalls.Add(callStatus.CallId, call);

				UpdateIncomingCall(callStatus);
				Subscribe(call);
			}
			finally
			{
				m_ParticipantsSection.Leave();
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

			ThinIncomingCall call = m_ParticipantsSection.Execute(() => m_IncomingCalls.GetDefault(callStatus.CallId, null));
			if (call == null)
				return;

			// Prevents overriding a resolved name with a number when the call disconnects
			if (callStatus.FarSiteName != callStatus.FarSiteNumber)
				call.Name = callStatus.FarSiteName;

			call.Number = callStatus.FarSiteNumber;

			bool? outgoing = callStatus.Outgoing;
			if (outgoing == null)
				call.Direction = eCallDirection.Incoming;
			else
				call.Direction = (bool)outgoing ? eCallDirection.Outgoing : eCallDirection.Incoming;

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

		#region Source Callbacks

		/// <summary>
		/// Subscribe to the source events.
		/// </summary>
		/// <param name="source"></param>
		private void Subscribe(ThinTraditionalParticipant source)
		{
			
			source.HoldCallback = HoldCallback;
			source.ResumeCallback = ResumeCallback;
			source.SendDtmfCallback = SendDtmfCallback;
			source.HangupCallback = HangupCallback;
		}

		/// <summary>
		/// Unsubscribe from the source events.
		/// </summary>
		/// <param name="source"></param>
		private void Unsubscribe(ThinTraditionalParticipant source)
		{
			source.HoldCallback = null;
			source.ResumeCallback = null;
			source.SendDtmfCallback = null;
			source.HangupCallback = null;
		}

		private void HangupCallback(ThinTraditionalParticipant sender)
		{
			int id = GetIdForSource(sender);

			m_DialComponent.HangupVideo(id);
		}

		private void SendDtmfCallback(ThinTraditionalParticipant sender, string data)
		{
			data.ForEach(c => m_DialComponent.Gendial(c));
		}

		private void ResumeCallback(ThinTraditionalParticipant sender)
		{
			m_RequestedHold = false;

			UpdateMute();
		}

		private void HoldCallback(ThinTraditionalParticipant sender)
		{
			m_RequestedHold = true;

			UpdateMute();
		}

		private int GetIdForSource(ThinTraditionalParticipant source)
		{
			return m_ParticipantsSection.Execute(() => m_Participants.GetKey(source));
		}

		#endregion

		#region Incoming Call Callbacks

		private void Subscribe(ThinIncomingCall call)
		{
			call.AnswerCallback = AnswerCallback;
			call.RejectCallback = RejectCallback;
		}

		private void Unsubscribe(ThinIncomingCall call)
		{
			call.AnswerCallback = null;
			call.RejectCallback = null;
		}

		private void RejectCallback(ThinIncomingCall sender)
		{
			int id = GetIdForIncomingCall(sender);

			m_DialComponent.HangupVideo(id);
		}

		private void AnswerCallback(ThinIncomingCall sender)
		{
			m_DialComponent.AnswerVideo();
		}

		private int GetIdForIncomingCall(ThinIncomingCall call)
		{
			return m_ParticipantsSection.Execute(() => m_IncomingCalls.GetKey(call));
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
	}
}
