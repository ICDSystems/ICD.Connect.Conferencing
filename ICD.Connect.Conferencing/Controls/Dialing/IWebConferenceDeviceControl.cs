using System;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.Conferences;

namespace ICD.Connect.Conferencing.Controls.Dialing
{
	public interface IWebConferenceDeviceControl : IConferenceDeviceControl<IWebConference>
	{
		void SetCameraEnabled(bool enabled);

		bool CameraEnabled { get; }

		event EventHandler<BoolEventArgs> OnCameraEnabledChanged;
	}
}