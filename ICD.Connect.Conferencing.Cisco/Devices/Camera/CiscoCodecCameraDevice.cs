using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Cameras;
using ICD.Connect.Cameras.Controls;
using ICD.Connect.Cameras.Devices;
using ICD.Connect.Conferencing.Cisco.Devices.Codec;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Cameras;
using ICD.Connect.Devices.EventArguments;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Conferencing.Cisco.Devices.Camera
{
	public sealed class CiscoCodecCameraDevice : AbstractCameraDevice<CiscoCodecCameraDeviceSettings>,
	                                             ICameraWithPanTilt, ICameraWithZoom, ICameraWithPresets
	{
		/// <summary>
		/// Raised when the parent codec changes.
		/// </summary>
		public event EventHandler OnCodecChanged;

		/// <summary>
		/// Raised when the presets are changed.
		/// </summary>
		public event EventHandler OnPresetsChanged;

		private CiscoCodecDevice m_Codec;
		private NearCamerasComponent m_CamerasComponent;
		private NearCamera m_Camera;
		private int? m_PanTiltSpeed;
		private int? m_ZoomSpeed;

		#region Properties

		public int CameraId { get; private set; }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public CiscoCodecCameraDevice()
		{
			Controls.Add(new GenericCameraRouteSourceControl<CiscoCodecCameraDevice>(this, 0));
			Controls.Add(new PanTiltControl<CiscoCodecCameraDevice>(this, 1));
			Controls.Add(new ZoomControl<CiscoCodecCameraDevice>(this, 2));
			Controls.Add(new PresetControl<CiscoCodecCameraDevice>(this, 3));
			Controls.Add(new CiscoCodecCameraDevicePowerControl(this, 4));
		}

		#region ICameraWithPanTilt

		/// <summary>
		/// Starts rotating the camera with the given action.
		/// </summary>
		/// <param name="action"></param>
		public void PanTilt(eCameraPanTiltAction action)
		{
			if (m_Camera == null)
				return;

			switch (action)
			{
				case eCameraPanTiltAction.Left:
					if (m_PanTiltSpeed == null)
						m_Camera.Pan(eCameraPan.Left);
					else
						m_Camera.Pan(eCameraPan.Left, m_PanTiltSpeed.Value);
					break;
				case eCameraPanTiltAction.Right:
					if (m_PanTiltSpeed == null)
						m_Camera.Pan(eCameraPan.Right);
					else
						m_Camera.Pan(eCameraPan.Right, m_PanTiltSpeed.Value);
					break;
				case eCameraPanTiltAction.Up:
					if (m_PanTiltSpeed == null)
						m_Camera.Tilt(eCameraTilt.Up);
					else
						m_Camera.Tilt(eCameraTilt.Up, m_PanTiltSpeed.Value);
					break;
				case eCameraPanTiltAction.Down:
					if (m_PanTiltSpeed == null)
						m_Camera.Tilt(eCameraTilt.Down);
					else
						m_Camera.Tilt(eCameraTilt.Down, m_PanTiltSpeed.Value);
					break;
				case eCameraPanTiltAction.Stop:
					m_Camera.StopPanTilt();
					break;
				default:
					throw new ArgumentOutOfRangeException("action");
			}
		}

		#endregion

		#region ICameraWithZoom

		/// <summary>
		/// Starts zooming the camera with the given action.
		/// </summary>
		/// <param name="action"></param>
		public void Zoom(eCameraZoomAction action)
		{
			if (m_Camera == null)
				return;

			switch (action)
			{
				case eCameraZoomAction.ZoomIn:
					if (m_ZoomSpeed == null)
						m_Camera.Zoom(eCameraZoom.In);
					else
						m_Camera.Zoom(eCameraZoom.In, m_ZoomSpeed.Value);
					break;
				case eCameraZoomAction.ZoomOut:
					if (m_ZoomSpeed == null)
						m_Camera.Zoom(eCameraZoom.Out);
					else
						m_Camera.Zoom(eCameraZoom.Out, m_ZoomSpeed.Value);
					break;
				case eCameraZoomAction.Stop:
					m_Camera.StopZoom();
					break;
				default:
					throw new ArgumentOutOfRangeException("action");
			}
		}

		#endregion

		#region ICameraWithPresets

		/// <summary>
		/// Gets the maximum number of presets this camera can support.
		/// </summary>
		public int MaxPresets { get { return 35; } }

		/// <summary>
		/// Gets the stored camera presets.
		/// </summary>
		public IEnumerable<CameraPreset> GetPresets()
		{
			return m_CamerasComponent.GetCameraPresets(CameraId);
		}

		/// <summary>
		/// Tells the camera to change its position to the given preset.
		/// </summary>
		/// <param name="presetId">The id of the preset to position to.</param>
		public void ActivatePreset(int presetId)
		{
			if (m_Camera == null)
				return;

			if (presetId < 1 || presetId > MaxPresets)
			{
				Log(eSeverity.Warning, "Camera preset must be between 1 and {0}, preset was not activated.", MaxPresets);
				return;
			}

			m_Camera.ActivatePreset(presetId);
		}

		/// <summary>
		/// Stores the cameras current position in the given preset index.
		/// </summary>
		/// <param name="presetId">The index to store the preset at.</param>
		public void StorePreset(int presetId)
		{
			if (m_Camera == null)
				return;

			if (presetId < 1 || presetId > MaxPresets)
			{
				Log(eSeverity.Warning, "Camera preset must be between 1 and {0}, preset was not stored.", MaxPresets);
				return;
			}

			m_Camera.StorePreset(presetId);
		}

		#endregion

		#region DeviceBase

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			return m_Codec != null && m_Codec.IsOnline;
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			OnCodecChanged = null;

			base.ClearSettingsFinal();

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
			{
				Log(eSeverity.Error, "No camera id set for camera: {0}", Name);
				return;
			}

			CameraId = (int)settings.CameraId;

			CiscoCodecDevice codec = null;
			if (settings.CodecId != null)
				codec = factory.GetOriginatorById<CiscoCodecDevice>(settings.CodecId.Value);

			SetCodec(codec);

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
			settings.CameraId = CameraId;
			settings.PanTiltSpeed = m_PanTiltSpeed;
			settings.ZoomSpeed = m_ZoomSpeed;
		}

		#endregion

		#region Public API

		[PublicAPI]
		public CiscoCodecDevice GetCodec()
		{
			return m_Codec;
		}

		[PublicAPI]
		public void SetCodec(CiscoCodecDevice codec)
		{
			if (codec == m_Codec)
				return;

			Unsubscribe(m_Codec);
			Unsubscribe(m_CamerasComponent);

			m_Codec = codec;
			m_CamerasComponent = m_Codec == null ? null : m_Codec.Components.GetComponent<NearCamerasComponent>();
			m_Camera = m_CamerasComponent == null ? null : m_CamerasComponent.GetCamera(CameraId);

			Subscribe(m_Codec);
			Subscribe(m_CamerasComponent);

			UpdateCachedOnlineStatus();

			OnCodecChanged.Raise(this);
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

			OnPresetsChanged.Raise(this);
		}

		#endregion
	}
}
