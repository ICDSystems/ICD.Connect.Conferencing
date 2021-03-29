using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.Camera
{
	public sealed class CameraComponent : AbstractZoomRoomComponent
	{
		public event EventHandler OnCamerasUpdated;
		public event EventHandler OnActiveCameraUpdated;

		public event EventHandler<CameraControlNotificationEventArgs> OnCameraControlNotification;
		// We don't get a notification when we give up control so it is tracked in a different event.
		public event EventHandler<StringEventArgs> OnZoomRoomGaveUpFarEndControl;

		private readonly IcdSortedDictionary<string, CameraInfo> m_Cameras;

		private string m_SelectedUsbId;

		/// <summary>
		/// Gets the active camera.
		/// </summary>
		[CanBeNull]
		public CameraInfo ActiveCamera
		{
			get { return m_SelectedUsbId == null ? null : m_Cameras.GetDefault(m_SelectedUsbId); }
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		public CameraComponent(ZoomRoom parent)
			: base(parent)
		{
			m_Cameras = new IcdSortedDictionary<string, CameraInfo>();

			Subscribe(parent);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal()
		{
			OnCamerasUpdated = null;
			OnActiveCameraUpdated = null;
			OnCameraControlNotification = null;
			OnZoomRoomGaveUpFarEndControl = null;

			base.DisposeFinal();

			Unsubscribe(Parent);
		}

		#region Methods

		protected override void Initialize()
		{
			Parent.SendCommand("zConfiguration Video Camera selectedId");
			Parent.SendCommand("zStatus Video Camera Line");
		}

		public IEnumerable<CameraInfo> GetCameras()
		{
			return m_Cameras.Values.ToArray(m_Cameras.Count);
		}

		public void SetActiveCameraByUsbId(string usbId)
		{
			Parent.Logger.Log(eSeverity.Informational, "Selecting video camera {0}", usbId);
			Parent.SendCommand("zConfiguration Video Camera selectedId: {0}", usbId);
		}

		public void SetCameraControlSpeed(string cameraId,
		                                  int speed)
		{
			if (speed < 1 || speed > 100)
			{
				Parent.Logger.Log(eSeverity.Warning, "Camera Control speed must be between 1 and 100. Speed: {0}", speed);
				return;
			}

			Parent.Logger.Log(eSeverity.Informational, "Setting camera with id {0} control speed to: {1}", cameraId, speed);
			Parent.SendCommand("zCommand Call CameraControl Id: {0} Speed: {1}", cameraId, speed);
		}

		public void ControlCamera(string cameraId, eCameraControlState state, eCameraControlAction action)
		{
			// Continues create a lot of log noise so we don't log them.
			if (state != eCameraControlState.Continue)
				Parent.Logger.Log(eSeverity.Informational,
				                  "Sending camera with id {0} control commands: State: {1} Action: {2}",
				                  cameraId, state, action);
			Parent.SendCommand("zCommand Call CameraControl Id: {0} State: {1} Action: {2}", cameraId, state, action);

			if (state == eCameraControlState.GiveUpRemote)
				OnZoomRoomGaveUpFarEndControl.Raise(this, new StringEventArgs(cameraId));
		}

		#endregion

		#region Parent Callbacks

		private void Subscribe(ZoomRoom parent)
		{
			parent.RegisterResponseCallback<VideoCameraLineResponse>(CameraListCallback);
			parent.RegisterResponseCallback<VideoConfigurationResponse>(SelectedCameraCallback);
			parent.RegisterResponseCallback<CameraControlNotificationResponse>(CameraControlNotificationCallback);
		}

		private void Unsubscribe(ZoomRoom parent)
		{
			parent.UnregisterResponseCallback<VideoCameraLineResponse>(CameraListCallback);
			parent.UnregisterResponseCallback<VideoConfigurationResponse>(SelectedCameraCallback);
			parent.UnregisterResponseCallback<CameraControlNotificationResponse>(CameraControlNotificationCallback);
		}

		private void CameraListCallback(ZoomRoom zoomroom, VideoCameraLineResponse response)
		{
			if (response.Cameras == null)
				return;

			m_Cameras.Clear();
			m_Cameras.AddRange(response.Cameras.Select(c => new KeyValuePair<string, CameraInfo>(c.UsbId, c)));

			OnCamerasUpdated.Raise(this);
		}

		private void SelectedCameraCallback(ZoomRoom zoomRoom, VideoConfigurationResponse response)
		{
			var video = response.Video;
			if (video == null)
				return;

			var camera = video.Camera;
			if (camera == null)
				return;

			m_SelectedUsbId = camera.SelectedId;

			OnActiveCameraUpdated.Raise(this);
		}

		private void CameraControlNotificationCallback(ZoomRoom zoomroom, CameraControlNotificationResponse response)
		{
			var notification = response.CameraControlNotification;
			if (notification == null)
				return;

			OnCameraControlNotification.Raise(this, new CameraControlNotificationEventArgs(notification));
		}

		#endregion

		#region Console

		public override string ConsoleHelp { get { return "Zoom Room Camera"; } }

		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new
				GenericConsoleCommand<string, int>("SetCameraControlSpeed",
				                                   "<string [camera id]> <int [control speed]>",
				                                   (id,
				                                    speed) =>
					                                   SetCameraControlSpeed(id,
					                                                         speed));

			yield return new
				GenericConsoleCommand<string, eCameraControlState, eCameraControlAction>("ControlCamera",
				                                                                         "<string [camera id]> <CameraControlState [Start|Continue|Stop|RequestRemote|GiveupRemote|RequestedByFarEnd]> <CameraControlAction [Left|Right|Up|Down|In|Out]>",
				                                                                         (id, state, action) =>
					                                                                         ControlCamera(id,
					                                                                                       state,
					                                                                                       action));
		}

		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
