using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.DialingPlans;
using ICD.Connect.Conferencing.Favorites;

namespace ICD.Connect.Conferencing.ConferenceManagers
{
	/// <summary>
	/// The ConferenceManager contains an IDialingPlan and a collection of IConferenceDeviceControls
	/// to place calls and manage the active conferences.
	/// </summary>
	public sealed class ConferenceManager : IConferenceManager, IDisposable
	{
		/// <summary>
		/// Raised when the Is Active state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnIsActiveChanged;

		/// <summary>
		/// Raised when the privacy mute status changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnPrivacyMuteStatusChange;

		/// <summary>
		/// Raised when the enforcement setting for do not disturb changes
		/// </summary>
		public event EventHandler<GenericEventArgs<eEnforceState>> OnEnforceDoNotDisturbChanged;

		/// <summary>
		/// Raised when the enforcement setting for auto answer changes
		/// </summary>
		public event EventHandler<GenericEventArgs<eEnforceState>> OnEnforceAutoAnswerChanged; 
		
        private readonly ConferenceManagerDialers m_Dialers;
		private readonly ConferenceManagerVolumePoints m_VolumePoints;
		private readonly DialingPlan m_DialingPlan;

		private eEnforceState m_EnforceDoNotDisturb;
		private eEnforceState m_EnforceAutoAnswer;
		private bool m_IsActive;
		private bool m_PrivacyMuted;

		#region Properties

		/// <summary>
		/// Indicates whether this conference manager should do anything. 
		/// True normally, false when the room that owns this conference manager has a parent combine room
		/// </summary>
		public bool IsActive
		{
			get { return m_IsActive; }
			set
			{
				if (value == m_IsActive)
					return;

				m_IsActive = value;

				OnIsActiveChanged.Raise(this, new BoolEventArgs(m_IsActive));
			}
		}

		/// <summary>
		/// Gets the conference manager dialers collection.
		/// </summary>
		[NotNull]
		public ConferenceManagerDialers Dialers { get { return m_Dialers; } }

		/// <summary>
		/// Gets the conference manager volume points collection.
		/// </summary>
		[NotNull]
		public ConferenceManagerVolumePoints VolumePoints { get { return m_VolumePoints; } }

		/// <summary>
		/// Gets the dialing plan.
		/// </summary>
		public DialingPlan DialingPlan { get { return m_DialingPlan; } }

		/// <summary>
		/// Gets/sets the favorites.
		/// </summary>
		public IFavorites Favorites { get; set; }

		/// <summary>
		/// Gets the logger.
		/// </summary>
		public ILoggerService Logger { get { return ServiceProvider.TryGetService<ILoggerService>(); } }

		/// <summary>
		/// Gets/sets the enforce do-not-disturb mode.
		/// </summary>
		public eEnforceState EnforceDoNotDisturb
		{
			get { return m_EnforceDoNotDisturb; }
			set
			{
				if (m_EnforceDoNotDisturb == value)
					return;

				m_EnforceDoNotDisturb = value;

				OnEnforceDoNotDisturbChanged.Raise(this, new GenericEventArgs<eEnforceState>(m_EnforceDoNotDisturb));
			}
		}

		/// <summary>
		/// Gets/sets the enforce auto answer mode.
		/// </summary>
		public eEnforceState EnforceAutoAnswer
		{
			get { return m_EnforceAutoAnswer; }
			set
			{
				if(m_EnforceAutoAnswer == value)
					return;

				m_EnforceAutoAnswer = value;

				OnEnforceAutoAnswerChanged.Raise(this, new GenericEventArgs<eEnforceState>(m_EnforceAutoAnswer));
			}
		}

		/// <summary>
		/// Gets/sets the privacy mute state.
		/// </summary>
		public bool PrivacyMuted
		{
			get { return m_PrivacyMuted; }
			set
			{
				if (value == m_PrivacyMuted)
					return;

				m_PrivacyMuted = value;

				OnPrivacyMuteStatusChange.Raise(this, new BoolEventArgs(m_PrivacyMuted));
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public ConferenceManager()
		{
			IsActive = true;

			m_DialingPlan = new DialingPlan();
			m_Dialers = new ConferenceManagerDialers(this);
			m_VolumePoints = new ConferenceManagerVolumePoints(this);
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			OnIsActiveChanged = null;
			OnEnforceDoNotDisturbChanged = null;
			OnEnforceAutoAnswerChanged = null;
			OnPrivacyMuteStatusChange = null;

			Clear();
		}

		/// <summary>
		/// Resets the conference manager back to its initial state.
		/// </summary>
		public void Clear()
		{
			Favorites = null;
			DialingPlan.ClearMatchers();
			Dialers.Clear();
			VolumePoints.Clear();
		}

		/// <summary>
		/// Dials the given number.
		/// </summary>
		/// <param name="dialContext"></param>
		public void Dial(IDialContext dialContext)
		{
			IConferenceDeviceControl conferenceControl = Dialers.GetBestDialer(dialContext);
			if (conferenceControl == null)
			{
				Logger.AddEntry(eSeverity.Error,
				                "{0} failed to dial {1} - No matching conference control could be found",
				                GetType().Name,
				                dialContext);
				return;
			}

			conferenceControl.Dial(dialContext);
		}

		#endregion
	}
}
