using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Nodes;
using ICD.Connect.Cameras;
using ICD.Connect.Cameras.Controls;
using ICD.Connect.Cameras.Devices;
using ICD.Connect.Conferencing.Cisco.Devices.Codec;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Cameras;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Video;
using ICD.Connect.Devices.EventArguments;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Conferencing.Cisco.Devices.Camera
{
	public sealed class CiscoCodecCameraDevice : AbstractCameraDevice<CiscoCodecCameraDeviceSettings>
	{
		#region Events

		/// <summary>
		/// Raised when the parent codec changes.
		/// </summary>
		public event EventHandler OnCodecChanged;

		/// <summary>
		/// Raised when the presets are changed.
		/// </summary>
		public override event EventHandler<GenericEventArgs<IEnumerable<CameraPreset>>> OnPresetsChanged;

		/// <summary>
		/// Raised when the mute state changes on the camera.
		/// </summary>
		public override event EventHandler<BoolEventArgs> OnCameraMuteStateChanged;
		
		#endregion

		#region Private Members

		[CanBeNull]
		private CiscoCodecDevice m_Codec;

		[CanBeNull]
		private NearCamerasComponent m_CamerasComponent;

		[CanBeNull]
		private NearCamera m_Camera;

		[CanBeNull]
		private VideoComponent m_VideoComponent;

		private int? m_PanTiltSpeed;
		private int? m_ZoomSpeed;
		private bool m_CameraMuted;

		#endregion

		#region Properties

		[PublicAPI]
		public CiscoCodecDevice Codec { get { return m_Codec; } }

		public int CameraId { get; private set; }

		[CanBeNull]
		public NearCamera Camera { get { return m_Camera; } }

		/// <summary>
		/// Gets whether the camera is currently muted
		/// </summary>
		public override bool IsCameraMuted { get { return m_CameraMuted; } }

		/// <summary>
		/// Gets the maximum number of presets this camera can support.
		/// </summary>
		public override int MaxPresets { get { return 35; } }

		private int PanSpeed { get { return m_PanTiltSpeed ?? (m_Camera == null ? 0 : m_Camera.PanSpeed); } }

		private int TiltSpeed { get { return m_PanTiltSpeed ?? (m_Camera == null ? 0 : m_Camera.TiltSpeed); } }

		private int ZoomSpeed { get { return m_ZoomSpeed ?? (m_Camera == null ? 0 : m_Camera.ZoomSpeed); } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public CiscoCodecCameraDevice()
		{
			Controls.Add(new GenericCameraRouteSourceControl<CiscoCodecCameraDevice>(this, 0));
			Controls.Add(new CameraDeviceControl(this, 1));
			Controls.Add(new CiscoCodecCameraDevicePowerControl(this, 2));
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			OnCodecChanged = null;
			OnPresetsChanged = null;

			base.DisposeFinal(disposing);
		}

		#region Methods

		[PublicAPI]
		public void SetCodec(CiscoCodecDevice codec)
		{
			if (codec == m_Codec)
				return;

			Unsubscribe(m_Codec);
			Unsubscribe(m_CamerasComponent);
			Unsubscribe(m_VideoComponent);

			m_Codec = codec;
			m_CamerasComponent = m_Codec == null ? null : m_Codec.Components.GetComponent<NearCamerasComponent>();
			m_Camera = m_CamerasComponent == null ? null : m_CamerasComponent.GetCamera(CameraId);
			m_VideoComponent = m_Codec == null ? null : m_Codec.Components.GetComponent<VideoComponent>();

			Subscribe(m_Codec);
			Subscribe(m_CamerasComponent);
			Subscribe(m_VideoComponent);

			UpdateCachedOnlineStatus();

			OnCodecChanged.Raise(this);
		}

		/// <summary>
		/// Starts panning the camera with the given action.
		/// </summary>
		/// <param name="action"></param>
		public override void Pan(eCameraPanAction action)
		{
			if (m_Camera == null)
				return;

			switch (action)
			{
				case eCameraPanAction.Left:
					m_Camera.Pan(eCameraPan.Left, PanSpeed);
					break;

				case eCameraPanAction.Right:
					m_Camera.Pan(eCameraPan.Right, PanSpeed);
					break;

				case eCameraPanAction.Stop:
					m_Camera.StopPanTilt();
					break;

				default:
					throw new ArgumentOutOfRangeException("action");
			}
		}

		/// <summary>
		/// Starts tilting the camera with the given action.
		/// </summary>
		/// <param name="action"></param>
		public override void Tilt(eCameraTiltAction action)
		{
			if (m_Camera == null)
				return;

			switch (action)
			{
				case eCameraTiltAction.Up:
					m_Camera.Tilt(eCameraTilt.Up, TiltSpeed);
					break;

				case eCameraTiltAction.Down:
					m_Camera.Tilt(eCameraTilt.Down, TiltSpeed);
					break;

				case eCameraTiltAction.Stop:
					m_Camera.StopPanTilt();
					break;

				default:
					throw new ArgumentOutOfRangeException("action");
			}
		}

		/// <summary>
		/// Starts zooming the camera with the given action.
		/// </summary>
		/// <param name="action"></param>
		public override void Zoom(eCameraZoomAction action)
		{
			if (m_Camera == null)
				return;

			switch (action)
			{
				case eCameraZoomAction.ZoomIn:
					m_Camera.Zoom(eCameraZoom.In, ZoomSpeed);
					break;

				case eCameraZoomAction.ZoomOut:
					m_Camera.Zoom(eCameraZoom.Out, ZoomSpeed);
					break;

				case eCameraZoomAction.Stop:
					m_Camera.StopZoom();
					break;

				default:
					throw new ArgumentOutOfRangeException("action");
			}
		}

		/// <summary>
		/// Gets the stored camera presets.
		/// </summary>
		public override IEnumerable<CameraPreset> GetPresets()
		{
			return m_CamerasComponent == null
				       ? Enumerable.Empty<CameraPreset>()
				       : m_CamerasComponent.GetRemappedCameraPresets(CameraId);
		}

		/// <summary>
		/// Tells the camera to change its position to the given preset.
		/// </summary>
		/// <param name="presetId">The id of the preset to position to.</param>
		public override void ActivatePreset(int presetId)
		{
			if (m_Camera == null)
				return;

			if (presetId < 1 || presetId > MaxPresets)
			{
				Log(eSeverity.Error, "Camera preset must be between 1 and {0}, preset was not activated.", MaxPresets);
				return;
			}

			m_Camera.ActivatePreset(presetId);
		}

		/// <summary>
		/// Stores the cameras current position in the given preset index.
		/// </summary>
		/// <param name="presetId">The index to store the preset at.</param>
		public override void StorePreset(int presetId)
		{
			if (m_Camera == null)
				return;

			if (presetId < 1 || presetId > MaxPresets)
			{
				Log(eSeverity.Error, "Camera preset must be between 1 and {0}, preset was not stored.", MaxPresets);
				return;
			}

			m_Camera.StorePreset(presetId);
		}

		/// <summary>
		/// Sets if the camera mute state should be active
		/// </summary>
		/// <param name="enable"></param>
		public override void MuteCamera(bool enable)
		{
			if(m_VideoComponent == null || m_CameraMuted == enable)
				return;

			m_VideoComponent.SetCameraMute(enable);
		}

		/// <summary>
		/// Resets camera to its predefined home position
		/// </summary>
		public override void SendCameraHome()
		{
			if(m_Camera == null)
				return;

			m_Camera.Reset();
		}

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			return m_Codec != null && m_Codec.IsOnline;
		}
		#endregion

		#region Codec Callbacks

		/// <summary>
		/// Subscribe to the codec events.
		/// </summary>
		/// <param name="codec"></param>
		private void Subscribe(CiscoCodecDevice codec)
		{
			if (codec == null)
				return;

			codec.OnIsOnlineStateChanged += CodecOnIsOnlineStateChanged;
		}

		/// <summary>
		/// Unsubscribe from the codec events.
		/// </summary>
		/// <param name="codec"></param>
		private void Unsubscribe(CiscoCodecDevice codec)
		{
			if (codec == null)
				return;

			codec.OnIsOnlineStateChanged -= CodecOnIsOnlineStateChanged;
		}

		/// <summary>
		/// Called when the codec changes online status.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void CodecOnIsOnlineStateChanged(object sender, DeviceBaseOnlineStateApiEventArgs args)
		{
			UpdateCachedOnlineStatus();
		}

		#endregion

		#region Cameras Component Callbacks

		/// <summary>
		/// Subscribe to the cameras component events.
		/// </summary>
		/// <param name="camerasComponent"></param>
		private void Subscribe(NearCamerasComponent camerasComponent)
		{
			if (camerasComponent == null)
				return;

			camerasComponent.OnPresetsChanged += CamerasComponentOnPresetsChanged;
		}

		/// <summary>
		/// Unsubscribe from the cameras component events.
		/// </summary>
		/// <param name="camerasComponent"></param>
		private void Unsubscribe(NearCamerasComponent camerasComponent)
		{
			if (camerasComponent == null)
				return;

			camerasComponent.OnPresetsChanged -= CamerasComponentOnPresetsChanged;
		}

		/// <summary>
		/// Called when the cameras component presets change.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void CamerasComponentOnPresetsChanged(object sender, IntEventArgs eventArgs)
		{
			if (eventArgs.Data != CameraId)
				return;

			OnPresetsChanged.Raise(this, new GenericEventArgs<IEnumerable<CameraPreset>>(GetPresets()));
		}

		#endregion

		#region Video Component Callbacks

		/// <summary>
		/// Subscribe to the video component events
		/// </summary>
		/// <param name="videoComponent"></param>
		private void Subscribe(VideoComponent videoComponent)
		{
			if (videoComponent == null)
				return;

			videoComponent.OnCamerasMutedChanged += VideoComponentOnCamerasMutedChanged;
		}

		/// <summary>
		/// Unsubscribe to the video component events
		/// </summary>
		/// <param name="videoComponent"></param>
		private void Unsubscribe(VideoComponent videoComponent)
		{
			if (videoComponent == null)
				return;

			videoComponent.OnCamerasMutedChanged -= VideoComponentOnCamerasMutedChanged;
		}

		private void VideoComponentOnCamerasMutedChanged(object sender, BoolEventArgs args)
		{
			if(m_CameraMuted == args.Data)
				return;

			m_CameraMuted = args.Data;

			OnCameraMuteStateChanged.Raise(this, new BoolEventArgs(m_CameraMuted));
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			m_PanTiltSpeed = null;
			m_ZoomSpeed = null;

			SupportedCameraFeatures = eCameraFeatures.None;

			CameraId = 0;
			SetCodec(null);
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(CiscoCodecCameraDeviceSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			if (settings.CameraId == null)
				Log(eSeverity.Error, "No Camera Id");

			CameraId = settings.CameraId ?? 0;

			CiscoCodecDevice codec = null;

			if (settings.CodecId.HasValue)
			{
				try
				{
					codec = factory.GetOriginatorById<CiscoCodecDevice>(settings.CodecId.Value);
				}
				catch (KeyNotFoundException)
				{
					Log(eSeverity.Error, "No Cisco Codec Device with id {0}", settings.CodecId.Value);
				}
			}

			SetCodec(codec);

			SupportedCameraFeatures = m_Camera == null ? eCameraFeatures.None :
						 eCameraFeatures.PanTiltZoom
					   | eCameraFeatures.Presets
					   | eCameraFeatures.Mute
					   | eCameraFeatures.Home;

			m_PanTiltSpeed = settings.PanTiltSpeed;
			m_ZoomSpeed = settings.ZoomSpeed;
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(CiscoCodecCameraDeviceSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.CodecId = m_Codec == null ? null : (int?)m_Codec.Id;
			settings.CameraId = CameraId == 0 ? (int?)null : CameraId;
			settings.PanTiltSpeed = m_PanTiltSpeed;
			settings.ZoomSpeed = m_ZoomSpeed;
		}

		#endregion

		#region Console

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Codec", Codec);
			addRow("Camera Id", CameraId);

			addRow("Pan Speed", PanSpeed);
			addRow("Tilt Speed", TiltSpeed);
			addRow("Zoom Speed", ZoomSpeed);
		}

		#endregion
	}
}
