using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.Devices;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
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
			Subscribe(parent);
		}

		private void Subscribe(IDialerDevice parent)
		{
			parent.OnAutoAnswerChanged += ParentOnAutoAnswerChanged;
			parent.OnDoNotDisturbChanged += ParentOnDoNotDisturbChanged;
			parent.OnPrivacyMuteChanged += ParentOnPrivacyMuteChanged;
			parent.OnParticipantAdded += ParentOnParticipantAdded;
			parent.OnParticipantRemoved += ParentOnParticipantRemoved;
			parent.OnIncomingCallAdded += ParentOnIncomingCallAdded;
			parent.OnIncomingCallRemoved += ParentOnIncomingCallRemoved;
		}

		private void ParentOnParticipantAdded(object sender, GenericEventArgs<ITraditionalParticipant> eventArgs)
		{
			IcdConsole.PrintLine(eConsoleColor.Magenta, "DialerDeviceDialerControl-ParentOnParticpantAdded-OnParticipantAdded");
			AddParticipant(eventArgs.Data);
		}

		private void ParentOnParticipantRemoved(object sender, GenericEventArgs<ITraditionalParticipant> eventArgs)
		{
			IcdConsole.PrintLine(eConsoleColor.Magenta, "DialerDeviceDialerControl-ParentOnParticpantRemoved-OnParticipantRemoved");
			RemoveParticipant(eventArgs.Data);
		}

		private void ParentOnIncomingCallAdded(object sender, GenericEventArgs<IIncomingCall> args)
		{
			
		}

		private void ParentOnIncomingCallRemoved(object sender, GenericEventArgs<IIncomingCall> args)
		{
			
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
