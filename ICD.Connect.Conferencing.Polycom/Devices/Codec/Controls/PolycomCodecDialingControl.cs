using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Calendaring;
using ICD.Connect.Calendaring.Booking;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.AutoAnswer;
using ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Dial;
using ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Mute;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec.Controls
{
	public sealed class PolycomCodecDialingControl : AbstractDialingDeviceControl<PolycomGroupSeriesDevice>
	{
		/// <summary>
		/// Raised when a source is added to the dialing component.
		/// </summary>
		public override event EventHandler<ConferenceSourceEventArgs> OnSourceAdded;

		/// <summary>
		/// Raised when a source is removed from the dialing component.
		/// </summary>
		public override event EventHandler<ConferenceSourceEventArgs> OnSourceRemoved;

		private readonly BiDictionary<int, ThinConferenceSource> m_Sources;
		private readonly SafeCriticalSection m_SourcesSection;

		private readonly DialComponent m_DialComponent;
		private readonly AutoAnswerComponent m_AutoAnswerComponent;
		private readonly MuteComponent m_MuteComponent;

		private bool m_RequestedPrivacyMute;
		private bool m_RequestedHold;

		#region Properties

		/// <summary>
		/// Gets the type of conference this dialer supports.
		/// </summary>
		public override eConferenceSourceType Supports { get { return eConferenceSourceType.Video | eConferenceSourceType.Audio; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public PolycomCodecDialingControl(PolycomGroupSeriesDevice parent, int id)
			: base(parent, id)
		{
			m_Sources = new BiDictionary<int, ThinConferenceSource>();
			m_SourcesSection = new SafeCriticalSection();

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
			OnSourceAdded = null;
			OnSourceRemoved = null;

			base.DisposeFinal(disposing);

			Unsubscribe(m_DialComponent);
			Unsubscribe(m_AutoAnswerComponent);
			Unsubscribe(m_MuteComponent);

			RemoveSources();
		}

		#region Methods

		/// <summary>
		/// Gets the active conference sources.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConferenceSource> GetSources()
		{
			return m_SourcesSection.Execute(() => m_Sources.Values.ToArray(m_Sources.Count));
		}

		/// <summary>
		/// Dials the given number.
		/// </summary>
		/// <param name="number"></param>
		public override void Dial(string number)
		{
			m_DialComponent.DialAuto(number);
		}

		/// <summary>
		/// Dials the given number.
		/// </summary>
		/// <param name="number"></param>
		/// <param name="callType"></param>
		public override void Dial(string number, eConferenceSourceType callType)
		{
			switch (callType)
			{
				case eConferenceSourceType.Audio:
					m_DialComponent.DialPhone(eDialProtocol.Auto, number);
					break;

				case eConferenceSourceType.Video:
					m_DialComponent.DialAuto(number);
					break;

				default:
					throw new ArgumentOutOfRangeException("callType");
			}
		}

		/// <summary>
		/// Returns the level of support the dialer has for the given booking.
		/// </summary>
		/// <param name="bookingNumber"></param>
		/// <returns></returns>
		public override eBookingSupport CanDial(IBookingNumber bookingNumber)
		{
			var sipNumber = bookingNumber as ISipBookingNumber;
			if (sipNumber != null && sipNumber.IsValidSipUri())
				return eBookingSupport.Supported;

			var potsNumber = bookingNumber as IPstnBookingNumber;
			if (potsNumber != null && !string.IsNullOrEmpty(potsNumber.PhoneNumber))
				return eBookingSupport.Supported;

			return eBookingSupport.Unsupported;
		}

		/// <summary>
		/// Dials the given booking.
		/// </summary>
		/// <param name="bookingNumber"></param>
		public override void Dial(IBookingNumber bookingNumber)
		{
			var sipNumber = bookingNumber as ISipBookingNumber;
			if (sipNumber != null && sipNumber.IsValidSipUri())
			{
				Dial(sipNumber.SipUri);
				return;
			}

			var potsNumber = bookingNumber as IPstnBookingNumber;
			if (potsNumber != null && !string.IsNullOrEmpty(potsNumber.PhoneNumber))
			{
				Dial(potsNumber.PhoneNumber);
				return;
			}
			
			Log(eSeverity.Error, "No supported methods for dialing the booking were found.");
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

			m_SourcesSection.Enter();

			try
			{
				// Clear out sources that are no longer online
				IcdHashSet<int> remove =
					m_Sources.Where(kvp => !statuses.ContainsKey(kvp.Key))
					         .Select(kvp => kvp.Key)
					         .ToIcdHashSet();
				RemoveSources(remove);

				// Update new/existing sources
				foreach (KeyValuePair<int, CallStatus> kvp in statuses)
				{
					if (m_Sources.ContainsKey(kvp.Key))
						UpdateSource(kvp.Value);

					switch (kvp.Value.ConnectionState)
					{
						case eConnectionState.Unknown:
							break;

						// Ignore inactive state, it's muddled around disconnected/disconnecting
						case eConnectionState.Inactive:
							break;

						case eConnectionState.Opened:
						case eConnectionState.Ringing:
						case eConnectionState.Connecting:
						case eConnectionState.Connected:
						case eConnectionState.Disconnecting:
							CreateSource(kvp.Value);
							break;

						case eConnectionState.Disconnected:
							RemoveSource(kvp.Key);
							break;
					}
				}
			}
			finally
			{
				m_SourcesSection.Leave();
			}
		}

		/// <summary>
		/// Removes all of the current sources.
		/// </summary>
		private void RemoveSources()
		{
			m_SourcesSection.Enter();

			try
			{
				RemoveSources(m_Sources.Keys.ToArray(m_Sources.Count));
			}
			finally
			{
				m_SourcesSection.Leave();
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
			ThinConferenceSource source;

			m_SourcesSection.Enter();

			try
			{
				if (!m_Sources.ContainsKey(id))
					return;

				source = m_Sources.GetValue(id);
				source.Status = eConferenceSourceStatus.Disconnected;

				SourceUnsubscribe(source);
				Unsubscribe(source);

				m_Sources.RemoveKey(id);

				// Leave hold state when out of calls
				if (m_Sources.Count == 0)
				{
					m_RequestedPrivacyMute = false;
					UpdateMute();
				}
			}
			finally
			{
				m_SourcesSection.Leave();
			}

			OnSourceRemoved.Raise(this, new ConferenceSourceEventArgs(source));
		}

		/// <summary>
		/// Creates a source for the given call status.
		/// </summary>
		/// <param name="callStatus"></param>
		private void CreateSource(CallStatus callStatus)
		{
			if (callStatus == null)
				throw new ArgumentNullException("callStatus");

			ThinConferenceSource source;

			m_SourcesSection.Enter();

			try
			{
				if (m_Sources.ContainsKey(callStatus.CallId))
					return;

				source = new ThinConferenceSource();
				m_Sources.Add(callStatus.CallId, source);

				UpdateSource(callStatus);

				SourceSubscribe(source);
				Subscribe(source);
			}
			finally
			{
				m_SourcesSection.Leave();
			}

			OnSourceAdded.Raise(this, new ConferenceSourceEventArgs(source));
		}

		/// <summary>
		/// Updates the source matching the given call status.
		/// </summary>
		/// <param name="callStatus"></param>
		private void UpdateSource(CallStatus callStatus)
		{
			if (callStatus == null)
				throw new ArgumentNullException("callStatus");

			ThinConferenceSource source = m_SourcesSection.Execute(() => m_Sources.GetDefault(callStatus.CallId, null));
			if (source == null)
				return;

			// Prevents overriding a resolved name with a number when the call disconnects
			if (callStatus.FarSiteName != callStatus.FarSiteNumber)
				source.Name = callStatus.FarSiteName;

			source.Number = callStatus.FarSiteNumber;

			bool? outgoing = callStatus.Outgoing;
			if (outgoing == null)
				source.Direction = eConferenceSourceDirection.Undefined;
			else
				source.Direction = (bool)outgoing ? eConferenceSourceDirection.Outgoing : eConferenceSourceDirection.Incoming;

			source.Status = GetStatus(callStatus.ConnectionState);
			source.SourceType = callStatus.VideoCall ? eConferenceSourceType.Video : eConferenceSourceType.Audio;

			if (source.GetIsOnline())
			{
				source.Start = source.Start ?? IcdEnvironment.GetLocalTime();

				if (source.AnswerState == default(eConferenceSourceAnswerState))
					source.AnswerState = eConferenceSourceAnswerState.Answered;
			}
			else
			{
				if (source.Start != null)
					source.End = source.End ?? IcdEnvironment.GetLocalTime();
			}
		}

		/// <summary>
		/// Gets the source status based on the given connection state.
		/// </summary>
		/// <param name="connectionState"></param>
		/// <returns></returns>
		private static eConferenceSourceStatus GetStatus(eConnectionState connectionState)
		{
			switch (connectionState)
			{
				case eConnectionState.Unknown:
					return eConferenceSourceStatus.Undefined;
				case eConnectionState.Opened:
				case eConnectionState.Ringing:
					return eConferenceSourceStatus.Ringing;
				case eConnectionState.Connecting:
					return eConferenceSourceStatus.Connecting;
				case eConnectionState.Connected:
					return eConferenceSourceStatus.Connected;
				case eConnectionState.Inactive:
				case eConnectionState.Disconnecting:
					return eConferenceSourceStatus.Disconnecting;
				case eConnectionState.Disconnected:
					return eConferenceSourceStatus.Disconnected;
				default:
					throw new ArgumentOutOfRangeException("connectionState");
			}
		}

		#endregion

		#region Source Callbacks

		/// <summary>
		/// Subscribe to the source events.
		/// </summary>
		/// <param name="source"></param>
		private void Subscribe(ThinConferenceSource source)
		{
			source.AnswerCallback = AnswerCallback;
			source.RejectCallback = RejectCallback;
			source.HoldCallback = HoldCallback;
			source.ResumeCallback = ResumeCallback;
			source.SendDtmfCallback = SendDtmfCallback;
			source.HangupCallback = HangupCallback;
		}

		/// <summary>
		/// Unsubscribe from the source events.
		/// </summary>
		/// <param name="source"></param>
		private void Unsubscribe(ThinConferenceSource source)
		{
			source.AnswerCallback = null;
			source.RejectCallback = null;
			source.HoldCallback = null;
			source.ResumeCallback = null;
			source.SendDtmfCallback = null;
			source.HangupCallback = null;
		}

		private void HangupCallback(ThinConferenceSource sender)
		{
			int id = GetIdForSource(sender);

			m_DialComponent.HangupVideo(id);
		}

		private void SendDtmfCallback(ThinConferenceSource sender, string data)
		{
			data.ForEach(c => m_DialComponent.Gendial(c));
		}

		private void ResumeCallback(ThinConferenceSource sender)
		{
			m_RequestedHold = false;

			UpdateMute();
		}

		private void HoldCallback(ThinConferenceSource sender)
		{
			m_RequestedHold = true;

			UpdateMute();
		}

		private void RejectCallback(ThinConferenceSource sender)
		{
			HangupCallback(sender);
		}

		private void AnswerCallback(ThinConferenceSource sender)
		{
			m_DialComponent.AnswerVideo();
		}

		private int GetIdForSource(ThinConferenceSource source)
		{
			return m_SourcesSection.Execute(() => m_Sources.GetKey(source));
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
