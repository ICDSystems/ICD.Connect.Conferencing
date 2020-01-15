using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.Conferencing.Zoom.Responses;

namespace ICD.Connect.Conferencing.Zoom.Components.Camera
{
	public sealed class CameraComponent : AbstractZoomRoomComponent
	{
		public event EventHandler OnCamerasUpdated;
		public event EventHandler OnActiveCameraUpdated;

		private readonly IcdOrderedDictionary<string, CameraInfo> m_Cameras;

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
			m_Cameras = new IcdOrderedDictionary<string, CameraInfo>();

			Subscribe(parent);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal()
		{
			OnCamerasUpdated = null;
			OnActiveCameraUpdated = null;

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
			Parent.Log(eSeverity.Informational, "Selecting video camera {0}", usbId);
			Parent.SendCommand("zConfiguration Video Camera selectedId: {0}", usbId);
		}

		public void SetCameraControlSpeed(string cameraId,
		                                  int speed)
		{
			if (speed < 1 || speed > 100)
			{
				Parent.Log(eSeverity.Warning, "Camera Control speed must be between 1 and 100. Speed: {0}", speed);
				return;
			}

			Parent.Log(eSeverity.Informational, "Setting camera with id {0} control speed to: {1}", cameraId, speed);
			Parent.SendCommand("zCommand Call CameraControl Id: {0} Speed: {1}", cameraId, speed);
		}

		public void ControlCamera(string cameraId, eCameraControlState state, eCameraControlAction action)
		{
			Parent.Log(eSeverity.Informational, "Sending camera with id {0} control commands: State: {1} Action: {2}",
			           cameraId, state, action);
			Parent.SendCommand("zCommand Call CameraControl Id: {0} State: {1} Action: {2}", cameraId, state, action);
		}

		#endregion

		#region Parent Callbacks

		private void Subscribe(ZoomRoom parent)
		{
			parent.RegisterResponseCallback<VideoCameraLineResponse>(CameraListCallback);
			parent.RegisterResponseCallback<VideoConfigurationResponse>(SelectedCameraCallback);
		}

		private void Unsubscribe(ZoomRoom parent)
		{
			parent.UnregisterResponseCallback<VideoCameraLineResponse>(CameraListCallback);
			parent.UnregisterResponseCallback<VideoConfigurationResponse>(SelectedCameraCallback);
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

		#endregion

		public override string ConsoleHelp { get { return "Zoom Room Camera"; } }

		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
			{
				yield return command;
			}

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
	}
}
