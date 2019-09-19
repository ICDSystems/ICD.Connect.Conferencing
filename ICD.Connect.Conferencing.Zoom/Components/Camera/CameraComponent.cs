using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
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
			Parent.SendCommand("zConfiguration Video Camera selectedId: {0}", usbId);
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
	}
}