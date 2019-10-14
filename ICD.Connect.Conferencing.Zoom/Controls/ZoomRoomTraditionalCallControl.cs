using System;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;
using ICD.Connect.Conferencing.Zoom.Components.TraditionalCall;

namespace ICD.Connect.Conferencing.Zoom.Controls
{
	public sealed class ZoomRoomTraditionalCallControl : AbstractTraditionalConferenceDeviceControl<ZoomRoom>
	{
		#region Events

		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallAdded;
		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallRemoved;

		#endregion

		public override eCallType Supports { get { return eCallType.Audio; } }

		public string ActiveCallId
		{
			get { return m_ActiveCallId; }
			private set
			{
				if (value == m_ActiveCallId)
					return;

				m_ActiveCallId = value;
				Parent.Log(eSeverity.Informational, "Setting ActiveCallID to {0}", m_ActiveCallId);
			}
		}

		private string m_ActiveCallId;
		private readonly TraditionalCallComponent m_CallComponent;

		#region Constructor

		public ZoomRoomTraditionalCallControl(ZoomRoom parent, int id) : base(parent, id)
		{
			m_CallComponent = Parent.Components.GetComponent<TraditionalCallComponent>();
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

			if (dialContext.Protocol == eDialProtocol.Pstn || dialContext.Protocol == eDialProtocol.Sip)
				return eDialContextSupport.Native;

			return eDialContextSupport.Unsupported;
		}

		public override void Dial(IDialContext dialContext)
		{
			switch (dialContext.Protocol)
			{
				case eDialProtocol.Pstn:
					PhoneCallOut(dialContext.DialString);
					break;

				default:
					Parent.Log(eSeverity.Warning, "Zoom Room traditional calls only support PSTN Currently");
					break;
			}
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
			Parent.SendCommand("zCommand Dial PhoneCallOut Number: {0}", dialString);
		}

		#endregion

		#region Call Component Callbacks

		private void Subscribe(TraditionalCallComponent callComponent)
		{
			callComponent.OnCallIdChanged += CallComponentOnCallIdChanged;
			callComponent.OnParticipantAdded += CallComponentOnParticipantAdded;
			callComponent.OnParticipantRemoved += CallComponentOnParticipantRemoved;
		}

		private void Unsubscribe(TraditionalCallComponent callComponent)
		{
			callComponent.OnCallIdChanged -= CallComponentOnCallIdChanged;
			callComponent.OnParticipantAdded -= CallComponentOnParticipantAdded;
			callComponent.OnParticipantRemoved -= CallComponentOnParticipantRemoved;
		}

		private void CallComponentOnCallIdChanged(object sender, StringEventArgs e)
		{
			var data = e.Data;
			if (data != null)
				ActiveCallId = data;
		}

		private void CallComponentOnParticipantAdded(object sender, ParticipantEventArgs e)
		{
			AddParticipant(e.Data as ITraditionalParticipant);
		}

		private void CallComponentOnParticipantRemoved(object sender, ParticipantEventArgs e)
		{
			RemoveParticipant(e.Data as ITraditionalParticipant);
		}

		#endregion
	}
}
