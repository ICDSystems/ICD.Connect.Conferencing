using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.AutoAnswer;
using ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Mute;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec.Controls
{
	public sealed class PolycomCodecDialingControl : AbstractDialingDeviceControl<PolycomGroupSeriesDevice>
	{
		/// <summary>
		/// Raised when a source is added to the dialing component.
		/// </summary>
		public override event EventHandler<ConferenceSourceEventArgs> OnSourceAdded;

		/// <summary>
		/// Raised when a source is removed from the dialing component.
		/// </summary>
		public override event EventHandler<ConferenceSourceEventArgs> OnSourceRemoved;

		private readonly AutoAnswerComponent m_AutoAnswerComponent;
		private readonly MuteComponent m_MuteComponent;

		/// <summary>
		/// Gets the type of conference this dialer supports.
		/// </summary>
		public override eConferenceSourceType Supports { get { return eConferenceSourceType.Video; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public PolycomCodecDialingControl(PolycomGroupSeriesDevice parent, int id)
			: base(parent, id)
		{
			m_AutoAnswerComponent = parent.Components.GetComponent<AutoAnswerComponent>();
			m_MuteComponent = parent.Components.GetComponent<MuteComponent>();

			Subscribe(m_AutoAnswerComponent);
			Subscribe(m_MuteComponent);
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

			Unsubscribe(m_AutoAnswerComponent);
			Unsubscribe(m_MuteComponent);
		}

		#region Methods

		/// <summary>
		/// Gets the active conference sources.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConferenceSource> GetSources()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Dials the given number.
		/// </summary>
		/// <param name="number"></param>
		public override void Dial(string number)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Dials the given number.
		/// </summary>
		/// <param name="number"></param>
		/// <param name="callType"></param>
		public override void Dial(string number, eConferenceSourceType callType)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Sets the do-not-disturb enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public override void SetDoNotDisturb(bool enabled)
		{
			// Don't leave Auto-Answer mode
			if (!enabled && m_AutoAnswerComponent.AutoAnswer == eAutoAnswer.Yes)
				return;

			m_AutoAnswerComponent.SetAutoAnswer(enabled ? eAutoAnswer.DoNotDisturb : eAutoAnswer.No);
		}

		/// <summary>
		/// Sets the auto-answer enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public override void SetAutoAnswer(bool enabled)
		{
			// Don't leave Do-Not-Disturb mode
			if (!enabled && m_AutoAnswerComponent.AutoAnswer == eAutoAnswer.DoNotDisturb)
				return;

			m_AutoAnswerComponent.SetAutoAnswer(enabled ? eAutoAnswer.Yes : eAutoAnswer.No);
		}

		/// <summary>
		/// Sets the privacy mute enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public override void SetPrivacyMute(bool enabled)
		{
			m_MuteComponent.MuteNear(enabled);
		}

		#endregion

		#region AutoAnswer Callbacks

		/// <summary>
		/// Subscribe to the component events.
		/// </summary>
		/// <param name="autoAnswerComponent"></param>
		private void Subscribe(AutoAnswerComponent autoAnswerComponent)
		{
			autoAnswerComponent.OnAutoAnswerChanged += AutoAnswerComponentOnAutoAnswerChanged;
		}

		/// <summary>
		/// Unsubscribe from the component events.
		/// </summary>
		/// <param name="autoAnswerComponent"></param>
		private void Unsubscribe(AutoAnswerComponent autoAnswerComponent)
		{
			autoAnswerComponent.OnAutoAnswerChanged -= AutoAnswerComponentOnAutoAnswerChanged;
		}

		/// <summary>
		/// Called when the autoanswer mode changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void AutoAnswerComponentOnAutoAnswerChanged(object sender, PolycomAutoAnswerEventArgs eventArgs)
		{
			AutoAnswer = m_AutoAnswerComponent.AutoAnswer == eAutoAnswer.Yes;
			DoNotDisturb = m_AutoAnswerComponent.AutoAnswer == eAutoAnswer.DoNotDisturb;
		}

		#endregion

		#region Mute Callbacks

		/// <summary>
		/// Subscribe to the component events.
		/// </summary>
		/// <param name="muteComponent"></param>
		private void Subscribe(MuteComponent muteComponent)
		{
			muteComponent.OnMutedNearChanged += MuteComponentOnMutedNearChanged;
		}

		/// <summary>
		/// Unsubscribe from the component events.
		/// </summary>
		/// <param name="muteComponent"></param>
		private void Unsubscribe(MuteComponent muteComponent)
		{
			muteComponent.OnMutedNearChanged -= MuteComponentOnMutedNearChanged;
		}

		/// <summary>
		/// Called when the near privacy mute state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="boolEventArgs"></param>
		private void MuteComponentOnMutedNearChanged(object sender, BoolEventArgs boolEventArgs)
		{
			PrivacyMuted = m_MuteComponent.MutedNear;
		}

		#endregion
	}
}
