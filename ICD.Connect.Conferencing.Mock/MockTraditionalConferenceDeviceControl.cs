using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.IncomingCalls;
using ICD.Connect.Conferencing.Participants;

namespace ICD.Connect.Conferencing.Mock
{
	public sealed class MockTraditionalConferenceDeviceControl : AbstractConferenceDeviceControl<IMockConferencingDevice, Conference>
	{
		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallAdded;
		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallRemoved;

		public override eCallType Supports { get { return eCallType.Video; } }

		public MockTraditionalConferenceDeviceControl(IMockConferencingDevice parent, int id)
			: base(parent, id)
		{
			SupportedConferenceControlFeatures =
				eConferenceControlFeatures.AutoAnswer |
				eConferenceControlFeatures.DoNotDisturb |
				eConferenceControlFeatures.PrivacyMute |
				eConferenceControlFeatures.CameraMute |
				eConferenceControlFeatures.Hold |
				eConferenceControlFeatures.Dtmf |
				eConferenceControlFeatures.CanDial |
				eConferenceControlFeatures.CanEnd;

			parent.OnParticipantAdded += ParentOnParticipantAdded;
			parent.OnParticipantRemoved += ParentOnParticipantRemoved;
			parent.OnIncomingCallAdded += ParentOnIncomingCallAdded;
			parent.OnIncomingCallRemoved += ParentOnIncomingCallRemoved;
		}

		private void ParentOnParticipantAdded(object sender, GenericEventArgs<IParticipant> eventArgs)
		{
			AddParticipant(eventArgs.Data);
		}

		private void ParentOnParticipantRemoved(object sender, GenericEventArgs<IParticipant> eventArgs)
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

		public override IEnumerable<Conference> GetConferences()
		{
			yield break;
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

		public override void SetCameraMute(bool mute)
		{
			CameraMute = mute;
		}

		public override void StartPersonalMeeting()
		{
			Parent.StartPersonalMeeting();
		}

		public override void EnableCallLock(bool enabled)
		{
			CallLock = enabled;
		}
	}
}
