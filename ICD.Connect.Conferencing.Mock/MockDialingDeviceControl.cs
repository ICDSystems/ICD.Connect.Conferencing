using System;
using System.Collections.Generic;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.Controls;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.Mock
{
	public sealed class MockDialingDeviceControl : AbstractDialingDeviceControl<IMockConferencingDevice>
	{
		public override event EventHandler<ConferenceSourceEventArgs> OnSourceAdded;
		public override event EventHandler<ConferenceSourceEventArgs> OnSourceRemoved;

		public override eConferenceSourceType Supports { get { return eConferenceSourceType.Video; } }

		public MockDialingDeviceControl(IMockConferencingDevice parent, int id)
			: base(parent, id)
		{
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

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnSourceAdded = null;
			OnSourceRemoved = null;

			base.DisposeFinal(disposing);
		}

		public override IEnumerable<IConferenceSource> GetSources()
		{
			return Parent.GetSources();
		}

		public override void Dial(string number)
		{
			Dial(number, eConferenceSourceType.Video);
		}

		public override void Dial(string number, eConferenceSourceType callType)
		{
			Parent.Dial(number, callType);
		}

		public override void SetDoNotDisturb(bool enabled)
		{
			if (DoNotDisturb == enabled)
				return;

			DoNotDisturb = enabled;

			Logger.AddEntry(eSeverity.Debug, "{0}:{1} Do Not Disturb set to {2}", Name, Id, DoNotDisturb);
		}

		public override void SetAutoAnswer(bool enabled)
		{
			if (AutoAnswer == enabled)
				return;

			AutoAnswer = enabled;

			Logger.AddEntry(eSeverity.Debug, "{0}:{1} Auto Answer set to {2}", Name, Id, AutoAnswer);
		}

		public override void SetPrivacyMute(bool enabled)
		{
			if (PrivacyMuted == enabled)
				return;

			PrivacyMuted = enabled;

			Logger.AddEntry(eSeverity.Debug, "{0}:{1} Privacy Mute set to {2}", Name, Id, PrivacyMuted);
		}
	}
}
