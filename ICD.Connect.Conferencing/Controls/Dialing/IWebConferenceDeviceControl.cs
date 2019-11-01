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
		/// Gets the camera enabled state.
		/// </summary>
		bool CameraEnabled { get; }

		/// <summary>
		/// Sets whether the camera should transmit video or not.
		/// </summary>
		/// <param name="enabled"></param>
		void SetCameraEnabled(bool enabled);

		/// <summary>
		/// Starts a personal meeting.
		/// </summary>
		void StartPersonalMeeting();
	}
}