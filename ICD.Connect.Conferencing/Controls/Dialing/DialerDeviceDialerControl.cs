using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.Devices;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.Controls.Dialing
{
	public sealed class DialerDeviceDialerControl : AbstractDialingDeviceControl<IDialerDevice> 
	{
		public override event EventHandler<ConferenceSourceEventArgs> OnSourceAdded;
		public override event EventHandler<ConferenceSourceEventArgs> OnSourceRemoved;

		public override eConferenceSourceType Supports { get { return eConferenceSourceType.Video; } }

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
			parent.OnSourceAdded += ParentOnSourceAdded;
			parent.OnSourceRemoved += ParentOnSourceRemoved;
		}

		private void ParentOnSourceAdded(object sender, ConferenceSourceEventArgs eventArgs)
		{
			SourceSubscribe(eventArgs.Data);
			OnSourceAdded.Raise(this, new ConferenceSourceEventArgs(eventArgs));
		}

		private void ParentOnSourceRemoved(object sender, ConferenceSourceEventArgs eventArgs)
		{
			SourceUnsubscribe(eventArgs.Data);
			OnSourceRemoved.Raise(this, new ConferenceSourceEventArgs(eventArgs));
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

		public override IEnumerable<IConferenceSource> GetSources()
		{
			return Parent.GetSources();
		}

		public override void Dial(string number)
		{
			Parent.Dial(number);
		}

		public override void Dial(string number, eConferenceSourceType callType)
		{
			Parent.Dial(number, callType);
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
