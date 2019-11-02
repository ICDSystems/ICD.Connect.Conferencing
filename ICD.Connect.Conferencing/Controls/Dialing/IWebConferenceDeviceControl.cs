using System;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.Conferences;

namespace ICD.Connect.Conferencing.Controls.Dialing
{
	public interface IWebConferenceDeviceControl : IConferenceDeviceControl<IWebConference>
	{
		/// <summary>
		/// Raised when the camera is enabled/disabled.
		/// </summary>
		event EventHandler<BoolEventArgs> OnCameraEnabledChanged;

		/// <summary>
		/// Raised when the call lock status changes.
		/// </summary>
		event EventHandler<BoolEventArgs> OnCallLockChanged;

		/// <summary>
		/// Raised when we start/stop being the host of the active conference.
		/// </summary>
		event EventHandler<BoolEventArgs> OnAmIHostChanged;

		/// <summary>
		/// Gets the camera enabled state.
		/// </summary>
		bool CameraEnabled { get; }

		/// <summary>
		/// Returns true if we are the host of the active conference.
		/// </summary>
		bool AmIHost { get; }

		/// <summary>
		/// Gets the CallLock State.
		/// </summary>
		bool CallLock { get; }

		/// <summary>
		/// Sets whether the camera should transmit video or not.
		/// </summary>
		/// <param name="enabled"></param>
		void SetCameraEnabled(bool enabled);

		/// <summary>
		/// Starts a personal meeting.
		/// </summary>
		void StartPersonalMeeting();

		/// <summary>
		/// Locks the current active conference so no more participants may join.
		/// </summary>
		/// <param name="enabled"></param>
		void EnableCallLock(bool enabled);
	}
}