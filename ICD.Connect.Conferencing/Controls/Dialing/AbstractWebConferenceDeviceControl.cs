using System;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Devices;
using ICD.Common.Logging.LoggingContexts;

namespace ICD.Connect.Conferencing.Controls.Dialing
{
	public abstract class AbstractWebConferenceDeviceControl<T> : AbstractConferenceDeviceControl<T, IWebConference>, IWebConferenceDeviceControl
		where T : IDevice
	{
		/// <summary>
		/// Raised when the camera is enabled/disabled.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnCameraEnabledChanged;

		/// <summary>
		/// Raised when the call lock status changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnCallLockChanged;

		/// <summary>
		/// Raised when we start/stop being the host of the active conference.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnAmIHostChanged;

		private readonly SafeCriticalSection m_StateSection;

		private bool m_CameraEnabled;
		private bool m_AmIHost;
		private bool m_CallLock;

		#region Properties

		/// <summary>
		/// Gets the camera enabled state.
		/// </summary>
		public bool CameraEnabled
		{
			get { return m_CameraEnabled;}
			protected set
			{
				m_StateSection.Enter();

				try
				{
					if (m_CameraEnabled == value)
						return;

					m_CameraEnabled = value;

					Logger.LogSetTo(eSeverity.Informational, "Camera Enabled", m_CameraEnabled);
				}
				finally
				{
					m_StateSection.Leave();
				}

				OnCameraEnabledChanged.Raise(this, new BoolEventArgs(value));
			}
		}

		/// <summary>
		/// Returns true if we are the host of the current conference.
		/// </summary>
		public bool AmIHost
		{
			get { return m_AmIHost; }
			protected set
			{
				m_StateSection.Enter();

				try
				{
					if (value == m_AmIHost)
						return;

					m_AmIHost = value;

					Logger.LogSetTo(eSeverity.Informational, "AmIHost", m_AmIHost);
				}
				finally
				{
					m_StateSection.Leave();
				}

				OnAmIHostChanged.Raise(this, new BoolEventArgs(m_AmIHost));
			}
		}

		/// <summary>
		/// Gets the CallLock State.
		/// </summary>
		public bool CallLock
		{
			get { return m_CallLock; }
			protected set
			{
				m_StateSection.Enter();

				try
				{
					if (value == m_CallLock)
						return;

					m_CallLock = value;

					Logger.LogSetTo(eSeverity.Informational, "CallLock", eSeverity.Informational);
					Activities.LogActivity(m_CallLock
						? new Activity(Activity.ePriority.Medium, "Call Lock", "Call Lock Enabled", eSeverity.Informational)
						: new Activity(Activity.ePriority.Low, "Call Lock", "Call Lock Disabled", eSeverity.Informational));
				}
				finally
				{
					m_StateSection.Leave();
				}

				OnCallLockChanged.Raise(this, new BoolEventArgs(m_CallLock));
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		protected AbstractWebConferenceDeviceControl(T parent, int id)
			: base(parent, id)
		{
			m_StateSection = new SafeCriticalSection();
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnCameraEnabledChanged = null;
			OnCallLockChanged = null;
			OnAmIHostChanged = null;

			base.DisposeFinal(disposing);
		}

		#region Methods

		/// <summary>
		/// Sets whether the camera should transmit video or not.
		/// </summary>
		/// <param name="enabled"></param>
		public abstract void SetCameraEnabled(bool enabled);

		/// <summary>
		/// Starts a personal meeting.
		/// </summary>
		public abstract void StartPersonalMeeting();

		/// <summary>
		/// Locks the current active conference so no more participants may join.
		/// </summary>
		/// <param name="enabled"></param>
		public abstract void EnableCallLock(bool enabled);

		#endregion
	}
}
