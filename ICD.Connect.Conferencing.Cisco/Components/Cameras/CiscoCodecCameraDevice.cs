using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Cameras;
using ICD.Connect.Cameras.Controls;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Conferencing.Cisco.Components.Cameras
{
	public sealed class CiscoCodecCameraDevice : AbstractCameraDevice<CiscoCodecCameraDeviceSettings>,
		ICameraWithPanTilt, ICameraWithZoom, ICameraWithPresets
	{
		#region Properties
		public int? CameraId { get; set; }
		private CiscoCodec m_Codec;
		private NearCamerasComponent m_CamerasComponent;
		private NearCamera m_Camera;
		#endregion

		public CiscoCodecCameraDevice()
		{
			Presets = new Dictionary<int, CameraPreset>();
			Controls.Add(new GenericCameraRouteSourceControl<CiscoCodecCameraDevice>(this, 0));
			Controls.Add(new PanTiltControl<CiscoCodecCameraDevice>(this, 1));
			Controls.Add(new ZoomControl<CiscoCodecCameraDevice>(this, 2));
			Controls.Add(new PresetControl<CiscoCodecCameraDevice>(this, 3));
		}


		#region ICameraWithPanTilt
		public void PanTilt(eCameraPanTiltAction action)
		{
			if (m_Codec == null)
			{
				return;
			}
			switch (action)
			{
				case eCameraPanTiltAction.Left:
					m_Camera.Pan(eCameraPan.Left);
					break;
				case eCameraPanTiltAction.Right:
					m_Camera.Pan(eCameraPan.Right);
					break;
				case eCameraPanTiltAction.Up:
					m_Camera.Tilt(eCameraTilt.Up);
					break;
				case eCameraPanTiltAction.Down:
					m_Camera.Tilt(eCameraTilt.Down);
					break;
				case eCameraPanTiltAction.Stop:
					m_Camera.Stop();
					break;
				default:
					throw new ArgumentOutOfRangeException("action");
			}
		}
		#endregion

		#region ICameraWithZoom
		public void Zoom(eCameraZoomAction action)
		{
			if (m_Codec == null)
			{
				return;
			}
			switch (action)
			{
				case eCameraZoomAction.ZoomIn:
					m_Camera.Zoom(eCameraZoom.In);
					break;
				case eCameraZoomAction.ZoomOut:
					m_Camera.Zoom(eCameraZoom.Out);
					break;
				case eCameraZoomAction.Stop:
					m_Camera.Stop();
					break;
				default:
					throw new ArgumentOutOfRangeException("action");
			}
		}
		#endregion

		#region ICameraWithPresets
		public int MaxPresets { get { return 35; } }
		public Dictionary<int, CameraPreset> Presets { get; private set; }
		public void ActivatePreset(int presetId)
		{
			if (m_Codec == null)
			{
				return;
			}
			if (presetId < 1 || presetId > MaxPresets)
			{
				return;
			}
			m_Camera.ActivatePreset(presetId);
		}

		public void StorePreset(int presetId)
		{
			if (m_Codec == null)
			{
				return;
			}
			if (presetId < 1 || presetId > MaxPresets)
			{
				Logger.AddEntry(eSeverity.Warning, "Camera preset must be between 1 and {0}, preset was not stored.", MaxPresets);
				return;
			}
			Presets.Add(presetId, new CameraPreset(presetId, CameraId == null ? 0 : CameraId.Value, presetId, string.Format("Preset{0}", presetId)));
			m_Camera.StorePreset(presetId);
		}
		#endregion

		#region DeviceBase
		protected override bool GetIsOnlineStatus()
		{
			if (m_Codec == null)
			{
				return false;
			}
			return true;
		}
		#endregion

		#region Settings

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(CiscoCodecCameraDeviceSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);
			CameraId = settings.CameraId;
			if (CameraId == null)
			{
				Logger.AddEntry(eSeverity.Error, "No camera id set for camera: {0}", Name);
				return;
			}
			if (settings.CodecId == null)
			{
				return;
			}
			m_Codec = factory.GetOriginatorById<CiscoCodec>(settings.CodecId.Value);
			if (m_Codec == null)
			{
				Logger.AddEntry(eSeverity.Error, "No codec device with id {0}", settings.CodecId.Value);
				return;
			}
			m_CamerasComponent = m_Codec.Components.GetComponent<NearCamerasComponent>();
			if (m_CamerasComponent == null)
			{
				Logger.AddEntry(eSeverity.Error, "No cameras component attached to codec: {0}", m_Codec.Name);
				return;
			}
			m_Camera = m_CamerasComponent.GetCamera(CameraId.Value);
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
		}
		#endregion

	}
}