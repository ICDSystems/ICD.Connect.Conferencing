using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Cameras;
using ICD.Connect.Cameras.Controls;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Conferencing.Cisco.Components.Cameras
{
	// ReSharper disable once ClassCanBeSealed.Global 
	//(this device has no inheritors, but its type is used as a filter for the power control so it cant be sealed)
	public class CiscoCodecCameraDevice : AbstractCameraDevice<CiscoCodecCameraDeviceSettings>,
		ICameraWithPanTilt, ICameraWithZoom, ICameraWithPresets
	{
		public event EventHandler CodecChanged;
		#region Properties
		private CiscoCodec m_Codec;
		private NearCamerasComponent m_CamerasComponent;
		private NearCamera m_Camera;
		private int? m_PanTiltSpeed;
		private int? m_ZoomSpeed;
		public int CameraId { get; private set; }
		#endregion

		public CiscoCodecCameraDevice()
		{
			Controls.Add(new GenericCameraRouteSourceControl<CiscoCodecCameraDevice>(this, 0));
			Controls.Add(new PanTiltControl<CiscoCodecCameraDevice>(this, 1));
			Controls.Add(new ZoomControl<CiscoCodecCameraDevice>(this, 2));
			Controls.Add(new PresetControl<CiscoCodecCameraDevice>(this, 3));
			Controls.Add(new CiscoCodecCameraDevicePowerControl(this, 4));
		}


		#region ICameraWithPanTilt
		public void PanTilt(eCameraPanTiltAction action)
		{
			if (m_Camera == null)
			{
				return;
			}
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
		public void Zoom(eCameraZoomAction action)
		{
			if (m_Camera == null)
			{
				return;
			}
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
		public int MaxPresets { get { return 35; } }

		public IEnumerable<CameraPreset> GetPresets()
		{
			return m_CamerasComponent.GetCameraPresets(CameraId);
		}

		public void ActivatePreset(int presetId)
		{
			if (m_Camera == null)
			{
				return;
			}
			if (presetId < 1 || presetId > MaxPresets)
			{
				Logger.AddEntry(eSeverity.Warning, "Camera preset must be between 1 and {0}, preset was not activated.", MaxPresets);
				return;
			}
			m_Camera.ActivatePreset(presetId);
		}

		public void StorePreset(int presetId)
		{
			if (m_Camera == null)
			{
				return;
			}
			if (presetId < 1 || presetId > MaxPresets)
			{
				Logger.AddEntry(eSeverity.Warning, "Camera preset must be between 1 and {0}, preset was not stored.", MaxPresets);
				return;
			}

			m_Camera.StorePreset(presetId);
		}
		#endregion

		#region DeviceBase
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
				Logger.AddEntry(eSeverity.Error, "No camera id set for camera: {0}", Name);
				return;
			}

			CameraId = (int)settings.CameraId;

			CiscoCodec codec = null;
			if (settings.CodecId != null)
				codec = factory.GetOriginatorById<CiscoCodec>(settings.CodecId.Value);

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
		public CiscoCodec GetCodec()
		{
			return m_Codec;
		}

		[PublicAPI]
		public void SetCodec(CiscoCodec codec)
		{
			if (codec == m_Codec)
				return;

			Unsubscribe(m_Codec);
			m_Codec = codec;

			Subscribe(m_Codec);
			UpdateCachedOnlineStatus();

			m_CamerasComponent = m_Codec == null ? null :m_Codec.Components.GetComponent<NearCamerasComponent>();

			m_Camera = m_CamerasComponent == null ? null : m_CamerasComponent.GetCamera(CameraId);

			CodecChanged.Raise(this);
		}
		#endregion

		#region Codec Callbacks

		private void Subscribe(CiscoCodec codec)
		{
			if (codec == null)
				return;

			codec.OnIsOnlineStateChanged += CodecOnIsOnlineStateChanged;
		}

		private void Unsubscribe(CiscoCodec codec)
		{
			if (codec == null)
				return;

			codec.OnIsOnlineStateChanged -= CodecOnIsOnlineStateChanged;
		}

		private void CodecOnIsOnlineStateChanged(object sender, BoolEventArgs boolEventArgs)
		{
			UpdateCachedOnlineStatus();
		}

		#endregion
	}
}