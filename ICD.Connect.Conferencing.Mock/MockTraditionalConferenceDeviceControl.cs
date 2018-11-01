using System;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;

namespace ICD.Connect.Conferencing.Mock
{
	public sealed class MockTraditionalConferenceDeviceControl : AbstractTraditionalConferenceDeviceControl<IMockConferencingDevice>
	{
		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallAdded;
		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallRemoved;

		public override eCallType Supports { get { return eCallType.Video; } }

		public MockTraditionalConferenceDeviceControl(IMockConferencingDevice parent, int id)
			: base(parent, id)
		{
			parent.OnParticipantAdded += ParentOnParticipantAdded;
			parent.OnParticipantRemoved += ParentOnParticipantRemoved;
			parent.OnIncomingCallAdded += ParentOnIncomingCallAdded;
			parent.OnIncomingCallRemoved += ParentOnIncomingCallRemoved;
		}

		private void ParentOnParticipantAdded(object sender, GenericEventArgs<ITraditionalParticipant> eventArgs)
		{
			AddParticipant(eventArgs.Data);
		}

		private void ParentOnParticipantRemoved(object sender, GenericEventArgs<ITraditionalParticipant> eventArgs)
		{
			RemoveParticipant(eventArgs.Data);
		}

		private void ParentOnIncomingCallAdded(object sender, GenericEventArgs<IIncomingCall> args)
		{
			OnIncomingCallAdded.Raise(this, new GenericEventArgs<IIncomingCall>(args.Data));
		}

		private void ParentOnIncomingCallRemoved(object sender, GenericEventArgs<IIncomingCall> args)
		{
			OnIncomingCallRemoved.Raise(this, new GenericEventArgs<IIncomingCall>(args.Data));
		}

		public override eDialContextSupport CanDial(IDialContext dialContext)
		{
			return Parent.CanDial(dialContext);
		}

		public override void Dial(IDialContext dialContext)
		{
			Parent.Dial(dialContext);
		}

		public override void SetDoNotDisturb(bool enabled)
		{
			DoNotDisturb = enabled;
		}

		public override void SetAutoAnswer(bool enabled)
		{
			AutoAnswer = enabled;
		}

		public override void SetPrivacyMute(bool enabled)
		{
			PrivacyMuted = enabled;
		}
	}
}
