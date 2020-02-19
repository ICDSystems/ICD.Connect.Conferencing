using System;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Devices;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.IncomingCalls;
using ICD.Connect.Conferencing.Participants;

namespace ICD.Connect.Conferencing.Controls.Dialing
{
	public sealed class DialerDeviceDialerControl : AbstractTraditionalConferenceDeviceControl<IDialerDevice>
	{
		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallAdded;
		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallRemoved;

		public override eCallType Supports { get { return eCallType.Video; } }

		public DialerDeviceDialerControl(IDialerDevice parent, int id)
			: base(parent, id)
		{
			SupportedConferenceFeatures =
				eConferenceFeatures.AutoAnswer |
				eConferenceFeatures.DoNotDisturb |
				eConferenceFeatures.PrivacyMute;
		}

		protected override void Subscribe(IDialerDevice parent)
		{
			base.Subscribe(parent);

			parent.OnAutoAnswerChanged += ParentOnAutoAnswerChanged;
			parent.OnDoNotDisturbChanged += ParentOnDoNotDisturbChanged;
			parent.OnPrivacyMuteChanged += ParentOnPrivacyMuteChanged;
			parent.OnParticipantAdded += ParentOnParticipantAdded;
			parent.OnParticipantRemoved += ParentOnParticipantRemoved;
			parent.OnIncomingCallAdded += ParentOnIncomingCallAdded;
			parent.OnIncomingCallRemoved += ParentOnIncomingCallRemoved;
		}

		protected override void Unsubscribe(IDialerDevice parent)
		{
			base.Unsubscribe(parent);

			parent.OnAutoAnswerChanged -= ParentOnAutoAnswerChanged;
			parent.OnDoNotDisturbChanged -= ParentOnDoNotDisturbChanged;
			parent.OnPrivacyMuteChanged -= ParentOnPrivacyMuteChanged;
			parent.OnParticipantAdded -= ParentOnParticipantAdded;
			parent.OnParticipantRemoved -= ParentOnParticipantRemoved;
			parent.OnIncomingCallAdded -= ParentOnIncomingCallAdded;
			parent.OnIncomingCallRemoved -= ParentOnIncomingCallRemoved;
		}

		private void ParentOnParticipantAdded(object sender, ParticipantEventArgs eventArgs)
		{
			AddParticipant(eventArgs.Data as ITraditionalParticipant);
		}

		private void ParentOnParticipantRemoved(object sender, ParticipantEventArgs eventArgs)
		{
			RemoveParticipant(eventArgs.Data as ITraditionalParticipant);
		}

		private void ParentOnIncomingCallAdded(object sender, GenericEventArgs<IIncomingCall> args)
		{
			OnIncomingCallAdded.Raise(this, new GenericEventArgs<IIncomingCall>(args.Data));
		}

		private void ParentOnIncomingCallRemoved(object sender, GenericEventArgs<IIncomingCall> args)
		{
			OnIncomingCallRemoved.Raise(this, new GenericEventArgs<IIncomingCall>(args.Data));
		}

		private void ParentOnPrivacyMuteChanged(object sender, BoolEventArgs eventArgs)
		{
			PrivacyMuted = Parent.PrivacyMuted;
		}

		private void ParentOnDoNotDisturbChanged(object sender, BoolEventArgs eventArgs)
		{
			DoNotDisturb = Parent.DoNotDisturb;
		}

		private void ParentOnAutoAnswerChanged(object sender, BoolEventArgs eventArgs)
		{
			AutoAnswer = Parent.AutoAnswer;
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
			Parent.SetDoNotDisturb(enabled);
		}

		public override void SetAutoAnswer(bool enabled)
		{
			Parent.SetAutoAnswer(enabled);
		}

		public override void SetPrivacyMute(bool enabled)
		{
			Parent.SetPrivacyMute(enabled);
		}
	}
}
