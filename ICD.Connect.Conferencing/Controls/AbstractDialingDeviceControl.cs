using System;
using System.Collections.Generic;
using ICD.Common.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Devices;
using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Conferencing.Controls
{
	public abstract class AbstractDialingDeviceControl<T> : AbstractDeviceControl<T>, IDialingDeviceControl
		where T : IDevice
	{
		public abstract event EventHandler<ConferenceSourceEventArgs> OnSourceAdded;

		public event EventHandler<BoolEventArgs> OnDoNotDisturbChanged;
		public event EventHandler<BoolEventArgs> OnAutoAnswerChanged;
		public event EventHandler<BoolEventArgs> OnPrivacyMuteChanged;

		private bool m_AutoAnswer;
		private bool m_PrivacyMuted;
		private bool m_DoNotDisturb;

		#region Properties

		/// <summary>
		/// Gets the AutoAnswer state.
		/// </summary>
		[PublicAPI]
		public bool AutoAnswer
		{
			get { return m_AutoAnswer; }
			protected set
			{
				if (value == m_AutoAnswer)
					return;

				m_AutoAnswer = value;

				OnAutoAnswerChanged.Raise(this, new BoolEventArgs(m_AutoAnswer));
			}
		}

		/// <summary>
		/// Gets the current microphone mute state.
		/// </summary>
		[PublicAPI]
		public bool PrivacyMuted
		{
			get { return m_PrivacyMuted; }
			protected set
			{
				if (value == m_PrivacyMuted)
					return;

				m_PrivacyMuted = value;

				OnPrivacyMuteChanged.Raise(this, new BoolEventArgs(m_PrivacyMuted));
			}
		}

		/// <summary>
		/// Gets the DoNotDisturb state.
		/// </summary>
		[PublicAPI]
		public bool DoNotDisturb
		{
			get { return m_DoNotDisturb; }
			protected set
			{
				if (value == m_DoNotDisturb)
					return;

				m_DoNotDisturb = value;

				OnDoNotDisturbChanged.Raise(this, new BoolEventArgs(m_DoNotDisturb));
			}
		}

		/// <summary>
		/// Gets the type of conference this dialer supports.
		/// </summary>
		public abstract eConferenceSourceType Supports { get; }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		protected AbstractDialingDeviceControl(T parent, int id)
			: base(parent, id)
		{
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnDoNotDisturbChanged = null;
			OnAutoAnswerChanged = null;
			OnPrivacyMuteChanged = null;

			base.DisposeFinal(disposing);
		}

		#region Methods

		/// <summary>
		/// Gets the active conference sources.
		/// </summary>
		/// <returns></returns>
		public abstract IEnumerable<IConferenceSource> GetSources();

		/// <summary>
		/// Dials the given number.
		/// </summary>
		/// <param name="number"></param>
		public abstract void Dial(string number);

		/// <summary>
		/// Dials the given number.
		/// </summary>
		/// <param name="number"></param>
		/// <param name="callType"></param>
		public abstract void Dial(string number, eConferenceSourceType callType);

		/// <summary>
		/// Sets the do-not-disturb enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public abstract void SetDoNotDisturb(bool enabled);

		/// <summary>
		/// Sets the auto-answer enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public abstract void SetAutoAnswer(bool enabled);

		/// <summary>
		/// Sets the privacy mute enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public abstract void SetPrivacyMute(bool enabled);

		#endregion
	}
}
