using System;
using System.Collections.Generic;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.Controls;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.Server
{
	public sealed class DialingDeviceClientControl : AbstractDialingDeviceControl<ConferencingClientDevice>, IDialingDeviceClientControl 
	{
		public DialingDeviceClientControl(ConferencingClientDevice parent, int id) : base(parent, id)
		{
		}

		public override event EventHandler<ConferenceSourceEventArgs> OnSourceAdded;

		public override eConferenceSourceType Supports { get { return eConferenceSourceType.Video; } }

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

		public void RaiseSourceAdded(IConferenceSource source)
		{
			OnSourceAdded.Raise(this, new ConferenceSourceEventArgs(source));
		}
	}
}
