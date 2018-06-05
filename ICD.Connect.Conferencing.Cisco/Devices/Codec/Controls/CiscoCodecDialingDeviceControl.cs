using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Dialing;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Controls
{
	public sealed class CiscoCodecDialingDeviceControl : AbstractDialingDeviceControl<CiscoCodecDevice>
	{
		/// <summary>
		/// Called when a source is added to the dialing component.
		/// </summary>
		public override event EventHandler<ConferenceSourceEventArgs> OnSourceAdded;
		public override event EventHandler<ConferenceSourceEventArgs> OnSourceRemoved;

		private readonly DialingComponent m_Component;

		/// <summary>
		/// Gets the human readable name for this control.
		/// </summary>
		public override string Name { get { return string.Format("{0} Dialing Control", Parent.Name); } }

		/// <summary>
		/// Gets the type of conference this dialer supports.
		/// </summary>
		public override eConferenceSourceType Supports { get { return eConferenceSourceType.Video; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public CiscoCodecDialingDeviceControl(CiscoCodecDevice parent, int id)
			: base(parent, id)
		{
			m_Component = Parent.Components.GetComponent<DialingComponent>();
			Subscribe(m_Component);

			UpdatePrivacyMute();
			UpdateDoNotDisturb();
			UpdateAutoAnswer();
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

			Unsubscribe(m_Component);
		}

		#region Methods

		/// <summary>
		/// Gets the active conference sources.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConferenceSource> GetSources()
		{
			return m_Component.GetSources();
		}

		/// <summary>
		/// Dials the given number.
		/// </summary>
		/// <param name="number"></param>
		public override void Dial(string number)
		{
			m_Component.Dial(number);
		}

		/// <summary>
		/// Dials the given number.
		/// </summary>
		/// <param name="number"></param>
		/// <param name="callType"></param>
		public override void Dial(string number, eConferenceSourceType callType)
		{
			m_Component.Dial(number, callType);
		}

		/// <summary>
		/// Sets the do-not-disturb enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public override void SetDoNotDisturb(bool enabled)
		{
			m_Component.SetDoNotDisturb(enabled);
		}

		/// <summary>
		/// Sets the auto-answer enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public override void SetAutoAnswer(bool enabled)
		{
			m_Component.SetAutoAnswer(enabled);
		}

		/// <summary>
		/// Sets the privacy mute enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public override void SetPrivacyMute(bool enabled)
		{
			m_Component.SetPrivacyMute(enabled);
		}

		#endregion

		#region Private Methods

		private void UpdatePrivacyMute()
		{
			PrivacyMuted = m_Component.PrivacyMuted;
		}

		private void UpdateDoNotDisturb()
		{
			DoNotDisturb = m_Component.DoNotDisturb;
		}

		private void UpdateAutoAnswer()
		{
			AutoAnswer = m_Component.AutoAnswer;
		}

		#endregion

		#region Component Callbacks

		private void Subscribe(DialingComponent component)
		{
			component.OnSourceAdded += ComponentOnSourceAdded;
			component.OnSourceRemoved += ComponentOnSourceRemoved;
			component.OnPrivacyMuteChanged += ComponentOnPrivacyMuteChanged;
			component.OnAutoAnswerChanged += ComponentOnAutoAnswerChanged;
			component.OnDoNotDisturbChanged += ComponentOnDoNotDisturbChanged;
		}

		private void Unsubscribe(DialingComponent component)
		{
			component.OnSourceAdded -= ComponentOnSourceAdded;
			component.OnSourceRemoved -= ComponentOnSourceRemoved;
			component.OnPrivacyMuteChanged -= ComponentOnPrivacyMuteChanged;
			component.OnAutoAnswerChanged -= ComponentOnAutoAnswerChanged;
			component.OnDoNotDisturbChanged -= ComponentOnDoNotDisturbChanged;
		}

		private void ComponentOnSourceAdded(object sender, ConferenceSourceEventArgs args)
		{
			SourceSubscribe(args.Data);
			OnSourceAdded.Raise(this, new ConferenceSourceEventArgs(args.Data));
		}

		private void ComponentOnSourceRemoved(object sender, ConferenceSourceEventArgs args)
		{
			SourceUnsubscribe(args.Data);
			OnSourceRemoved.Raise(this, new ConferenceSourceEventArgs(args.Data));
		}

		private void ComponentOnPrivacyMuteChanged(object sender, BoolEventArgs args)
		{
			UpdatePrivacyMute();
		}

		private void ComponentOnDoNotDisturbChanged(object sender, BoolEventArgs args)
		{
			UpdateDoNotDisturb();
		}

		private void ComponentOnAutoAnswerChanged(object sender, BoolEventArgs args)
		{
			UpdateAutoAnswer();
		}

		#endregion
	}
}
