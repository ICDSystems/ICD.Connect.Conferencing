using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.Controls;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.Server
{
	public sealed class DialingDeviceClientControl : AbstractDialingDeviceControl<IConferencingClientDevice>, IDialingDeviceClientControl 
	{
		public override event EventHandler<ConferenceSourceEventArgs> OnSourceAdded;

		public override eConferenceSourceType Supports { get { return eConferenceSourceType.Video; } }

		public DialingDeviceClientControl(ConferencingClientDevice parent, int id)
			: base(parent, id)
		{
			Subscribe(parent);
		}

		private void Subscribe(ConferencingClientDevice parent)
		{
			parent.OnAutoAnswerChanged += ParentOnAutoAnswerChanged;
			parent.OnDoNotDisturbChanged += ParentOnDoNotDisturbChanged;
			parent.OnPrivacyMuteChanged += ParentOnPrivacyMuteChanged;
			parent.OnSourceAdded += ParentOnSourceAdded;
		}

		private void ParentOnSourceAdded(object sender, ConferenceSourceEventArgs eventArgs)
		{
			OnSourceAdded.Raise(this, new ConferenceSourceEventArgs(eventArgs));
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
			//TODO: Dialing for Interpretation to be supported in the future, but currently out of spec
		}

		public override void Dial(string number, eConferenceSourceType callType)
		{
			//TODO: Dialing for Interpretation to be supported in the future, but currently out of spec
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
