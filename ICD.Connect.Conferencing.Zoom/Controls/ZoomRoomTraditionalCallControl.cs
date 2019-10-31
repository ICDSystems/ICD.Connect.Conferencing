using System;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;
using ICD.Connect.Conferencing.Utils;
using ICD.Connect.Conferencing.Zoom.Components.TraditionalCall;
using ICD.Connect.Conferencing.Zoom.Responses;

namespace ICD.Connect.Conferencing.Zoom.Controls
{
	public sealed class ZoomRoomTraditionalCallControl : AbstractTraditionalConferenceDeviceControl<ZoomRoom>
	{
		#region Events

		/// <summary>
		/// Raised when a source property changes.
		/// </summary>
		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallAdded;

		/// <summary>
		/// Raised when a source property changes.
		/// </summary>
		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallRemoved;

		#endregion

		private readonly TraditionalCallComponent m_CallComponent;
		private readonly IcdOrderedDictionary<string, ThinTraditionalParticipant> m_CallIdToParticipant;
		private readonly SafeCriticalSection m_ParticipantSection;

		public override eCallType Supports { get { return eCallType.Audio; } }

		#region Constructor

		public ZoomRoomTraditionalCallControl(ZoomRoom parent, int id)
			: base(parent, id)
		{
			m_CallComponent = Parent.Components.GetComponent<TraditionalCallComponent>();
			m_CallIdToParticipant = new IcdOrderedDictionary<string, ThinTraditionalParticipant>();
			m_ParticipantSection = new SafeCriticalSection();

			Subscribe(m_CallComponent);
		}

		protected override void DisposeFinal(bool disposing)
		{
			OnIncomingCallAdded = null;
			OnIncomingCallRemoved = null;

			Unsubscribe(m_CallComponent);

			base.DisposeFinal(disposing);
		}

		#endregion

		#region Methods

		public override eDialContextSupport CanDial(IDialContext dialContext)
		{
			if (string.IsNullOrEmpty(dialContext.DialString))
				return eDialContextSupport.Unsupported;

			if (dialContext.Protocol == eDialProtocol.Sip && SipUtils.IsValidSipUri(dialContext.DialString))
				return eDialContextSupport.Supported;

			if (dialContext.Protocol == eDialProtocol.Pstn)
				return eDialContextSupport.Supported;

			if (dialContext.Protocol == eDialProtocol.Unknown)
				return eDialContextSupport.Unknown;

			return eDialContextSupport.Unsupported;
		}

		public override void Dial(IDialContext dialContext)
		{
			if (CanDial(dialContext) == eDialContextSupport.Unsupported)
				throw new ArgumentException("Zoom Room traditional calls only support PSTN Currently", "dialContext");

			PhoneCallOut(dialContext.DialString);
		}

		public override void SetDoNotDisturb(bool enabled)
		{
			Parent.DoNotDisturb = enabled;
		}

		public override void SetAutoAnswer(bool enabled)
		{
			Parent.Log(eSeverity.Warning, "Zoom Room does not support setting auto-answer through the SSH API");
		}

		public override void SetPrivacyMute(bool enabled)
		{
			Parent.Log(eSeverity.Warning, "Zoom Room does not support setting PSTN privacy mute outside of meetings");
		}

		#endregion

		#region Private Methods

		private void PhoneCallOut(string dialString)
		{
			if (m_CallIdToParticipant.Any())
				throw new InvalidOperationException("Zoom Room only supports singular call out");

			m_CallComponent.PhoneCallOut(dialString);
		}

		private void Hangup(string callId)
		{
			if (!m_CallIdToParticipant.Any())
				throw new InvalidOperationException("No active call to hangup");

			m_CallComponent.Hangup(callId);
		}

		private void SendDtmf(string callId, char data)
		{
			if (!m_CallIdToParticipant.Any())
				throw new InvalidOperationException("No active call to send DTMF data to");

			m_CallComponent.SendDtmf(callId, data);
		}

		/// <summary>
		/// Creates and/or updates a participant from the given call info.
		/// </summary>
		/// <param name="info"></param>
		private void CreateOrUpdateCall(TraditionalZoomPhoneCallInfo info)
		{
			if (info == null)
				throw new ArgumentNullException("info");

			ThinTraditionalParticipant value;

			m_ParticipantSection.Enter();

			try
			{
				if (!m_CallIdToParticipant.TryGetValue(info.CallId, out value))
				{
					value = new ThinTraditionalParticipant();
					m_CallIdToParticipant.Add(info.CallId, value);
					Subscribe(value);
				}

				UpdateCall(info);
			}
			finally
			{
				m_ParticipantSection.Leave();
			}

			AddParticipant(value);
		}

		/// <summary>
		/// Removes the participants from the call info Id.
		/// </summary>
		/// <param name="info"></param>
		private void RemoveCall(TraditionalZoomPhoneCallInfo info)
		{
			ThinTraditionalParticipant value;

			m_ParticipantSection.Enter();

			try
			{
				if (!m_CallIdToParticipant.TryGetValue(info.CallId, out value))
					return;

				value.End = value.End ?? IcdEnvironment.GetLocalTime();
				value.Status = eParticipantStatus.Disconnected;

				Unsubscribe(value);

				m_CallIdToParticipant.Remove(info.CallId);
			}
			finally
			{
				m_ParticipantSection.Leave();
			}

			RemoveParticipant(value);
		}

		/// <summary>
		/// Updates the participant with the given call info.
		/// </summary>
		/// <param name="info"></param>
		private void UpdateCall(TraditionalZoomPhoneCallInfo info)
		{
			if (info == null)
				throw new ArgumentNullException("info");

			ThinTraditionalParticipant value = m_ParticipantSection.Execute(() => m_CallIdToParticipant.GetDefault(info.CallId));
			if (value == null)
				return;

			value.Number = info.PeerNumber;
			value.Name = info.PeerDisplayName;
			value.Direction = info.IsIncomingCall ? eCallDirection.Incoming : eCallDirection.Outgoing;
			value.Status = GetStatus(info.Status);
			value.CallType = Supports;

			if (value.GetIsOnline())
				value.Start = value.Start ?? IcdEnvironment.GetLocalTime();
		}

		/// <summary>
		/// Gets the participant status based on the zoom call status.
		/// </summary>
		/// <param name="status"></param>
		/// <returns></returns>
		private static eParticipantStatus GetStatus(eZoomPhoneCallStatus status)
		{
			switch (status)
			{
				case eZoomPhoneCallStatus.None:
				case eZoomPhoneCallStatus.NotFound:
				case eZoomPhoneCallStatus.Incoming:
					return eParticipantStatus.Undefined;
				case eZoomPhoneCallStatus.Ringing:
					return eParticipantStatus.Ringing;
				case eZoomPhoneCallStatus.Init:
					return eParticipantStatus.Connecting;
				case eZoomPhoneCallStatus.InCall:
					return eParticipantStatus.Connected;
				default:
					throw new ArgumentOutOfRangeException("status");
			}
		}

		#endregion

		#region Call Component Callbacks

		private void Subscribe(TraditionalCallComponent callComponent)
		{
			callComponent.OnCallStatusChanged += CallComponentOnCallStatusChanged;
			callComponent.OnCallTerminated += CallComponentOnCallTerminated;
		}

		private void Unsubscribe(TraditionalCallComponent callComponent)
		{
			callComponent.OnCallStatusChanged -= CallComponentOnCallStatusChanged;
			callComponent.OnCallTerminated -= CallComponentOnCallTerminated;
		}

		private void CallComponentOnCallStatusChanged(object sender, GenericEventArgs<TraditionalZoomPhoneCallInfo> e)
		{
			TraditionalZoomPhoneCallInfo data = e.Data;
			if (data == null)
				return;

			switch (data.Status)
			{
				case eZoomPhoneCallStatus.None:
				case eZoomPhoneCallStatus.NotFound:
					break;

				// Zoom doesn't support answering incoming calls so we pretend they don't exist
				case eZoomPhoneCallStatus.Incoming:
					break;

				case eZoomPhoneCallStatus.Ringing:
				case eZoomPhoneCallStatus.Init:
				case eZoomPhoneCallStatus.InCall:
					CreateOrUpdateCall(data);
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void CallComponentOnCallTerminated(object sender, GenericEventArgs<TraditionalZoomPhoneCallInfo> e)
		{
			TraditionalZoomPhoneCallInfo data = e.Data;
			if (data != null)
				RemoveCall(data);
		}

		#endregion

		#region Participant Callbacks

		/// <summary>
		/// Subscribe to the participant events.
		/// </summary>
		/// <param name="value"></param>
		private void Subscribe(ThinTraditionalParticipant value)
		{
			value.HangupCallback = HangupCallback;
			value.SendDtmfCallback = SendDtmfCallback;
			value.HoldCallback = HoldCallback;
			value.ResumeCallback = ResumeCallback;
		}

		/// <summary>
		/// Unsubscribe from the participant events.
		/// </summary>
		/// <param name="value"></param>
		private void Unsubscribe(ThinTraditionalParticipant value)
		{
			value.HangupCallback = null;
			value.SendDtmfCallback = null;
			value.HoldCallback = null;
			value.ResumeCallback = null;
		}

		private void SendDtmfCallback(ThinTraditionalParticipant sender, string data)
		{
			string callId = GetIdForParticipant(sender);

			foreach (char index in data)
				SendDtmf(callId, index);
		}

		private void HangupCallback(ThinTraditionalParticipant sender)
		{
			string callId = GetIdForParticipant(sender);

			Hangup(callId);
		}

		private string GetIdForParticipant(ThinTraditionalParticipant value)
		{
			return m_ParticipantSection.Execute(() => m_CallIdToParticipant.GetKey(value));
		}

		private void HoldCallback(ThinTraditionalParticipant sender)
		{
			Parent.Log(eSeverity.Warning, "Zoom Room PSTN does not support call hold/resume feature");

		}

		private void ResumeCallback(ThinTraditionalParticipant sender)
		{
			Parent.Log(eSeverity.Warning, "Zoom Room PSTN does not support call hold/resume feature");
		}

		#endregion
	}
}
