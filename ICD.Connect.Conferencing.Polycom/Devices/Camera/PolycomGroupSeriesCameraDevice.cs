using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Cameras;
using ICD.Connect.Cameras.Controls;
using ICD.Connect.Cameras.Devices;
using ICD.Connect.Conferencing.Polycom.Devices.Codec;
using ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Camera;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Devices.EventArguments;
using ICD.Connect.Settings;

namespace ICD.Connect.Conferencing.Polycom.Devices.Camera
{
	public sealed class PolycomGroupSeriesCameraDevice : AbstractCameraDevice<PolycomGroupSeriesCameraSettings>
	{
		/// <summary>
		/// Raised when the parent codec device changes.
		/// </summary>
		public event EventHandler OnCodecChanged;

		private PolycomGroupSeriesDevice m_Codec;
		private CameraComponent m_CameraComponent;

		#region Properties

		/// <summary>
		/// Gets the maximum number of presets this camera can support.
		/// </summary>
		public override int MaxPresets
		{
			get
			{
				// TODO - This camera actually supports 0-99 inclusive for a total of 100, need to improve presets interface
				return 99;
			}
		}

		/// <summary>
		/// Gets the id for the camera being controlled.
		/// </summary>
		public int CameraId { get; private set; }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public PolycomGroupSeriesCameraDevice()
		{
			SupportedCameraFeatures =
				eCameraFeatures.PanTiltZoom |
				eCameraFeatures.Presets;
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			OnCodecChanged = null;

			base.DisposeFinal(disposing);
		}

		#region Methods

		/// <summary>
		/// Gets the parent codec device.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		public PolycomGroupSeriesDevice GetCodec()
		{
			return m_Codec;
		}

		/// <summary>
		/// Sets the parent codec device.
		/// </summary>
		/// <param name="codec"></param>
		[PublicAPI]
		public void SetCodec(PolycomGroupSeriesDevice codec)
		{
			if (codec == m_Codec)
				return;

			Unsubscribe(m_Codec);

			m_Codec = codec;
			m_CameraComponent = m_Codec == null ? null : m_Codec.Components.GetComponent<CameraComponent>();

			Subscribe(m_Codec);

			UpdateCachedOnlineStatus();

			OnCodecChanged.Raise(this);
		}

		/// <summary>
		/// Begins panning the camera
		/// </summary>
		/// <param name="action"></param>
		public override void Pan(eCameraPanAction action)
		{
			if (m_CameraComponent == null)
				return;

			switch (action)
			{
				case eCameraPanAction.Left:
					m_CameraComponent.MoveNear(CameraId, eCameraAction.Left);
					break;
				case eCameraPanAction.Right:
					m_CameraComponent.MoveNear(CameraId, eCameraAction.Right);
					break;
				case eCameraPanAction.Stop:
					m_CameraComponent.MoveNear(CameraId, eCameraAction.Stop);
					break;
				default:
					throw new ArgumentOutOfRangeException("action");
			}
		}

		/// <summary>
		/// Begin tilting the camera.
		/// </summary>
		public override void Tilt(eCameraTiltAction action)
		{
			if (m_CameraComponent == null)
				return;

			switch (action)
			{
				case eCameraTiltAction.Up:
					m_CameraComponent.MoveNear(CameraId, eCameraAction.Up);
					break;
				case eCameraTiltAction.Down:
					m_CameraComponent.MoveNear(CameraId, eCameraAction.Down);
					break;
				case eCameraTiltAction.Stop:
					m_CameraComponent.MoveNear(CameraId, eCameraAction.Stop);
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
			if (m_CameraComponent == null)
				return;

			switch (action)
			{
				case eCameraZoomAction.ZoomIn:
					m_CameraComponent.MoveNear(CameraId, eCameraAction.ZoomIn);
					break;
				case eCameraZoomAction.ZoomOut:
					m_CameraComponent.MoveNear(CameraId, eCameraAction.ZoomOut);
					break;
				case eCameraZoomAction.Stop:
					m_CameraComponent.MoveNear(CameraId, eCameraAction.Stop);
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
			return Enumerable.Range(0, 100).Select(i => new CameraPreset(i, string.Format("Preset {0}", i)));
		}

		/// <summary>
		/// Tells the camera to change its position to the given preset.
		/// </summary>
		/// <param name="presetId">The id of the preset to position to.</param>
		public override void ActivatePreset(int presetId)
		{
			m_CameraComponent.GoNearCameraPreset(CameraId, presetId);
		}

		/// <summary>
		/// Stores the cameras current position in the given preset index.
		/// </summary>
		/// <param name="presetId">The index to store the preset at.</param>
		public override void StorePreset(int presetId)
		{
			m_CameraComponent.SetNearCameraPreset(CameraId, presetId);
		}

		/// <summary>
		/// Sets if the camera mute state should be active
		/// </summary>
		/// <param name="enable"></param>
		public override void MuteCamera(bool enable)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Resets camera to its predefined home position
		/// </summary>
		public override void ActivateHome()
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Stores the current position as the home position.
		/// </summary>
		public override void StoreHome()
		{
			throw new NotSupportedException();
		}

		#endregion

		#region Private Methods

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
			base.ClearSettingsFinal();

			CameraId = 0;
			SetCodec(null);
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(PolycomGroupSeriesCameraSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			if (settings.CameraId == null)
			{
				Logger.Log(eSeverity.Error, "No camera id set for camera: {0}", Name);
				return;
			}

			CameraId = (int)settings.CameraId;

			PolycomGroupSeriesDevice codec = null;
			if (settings.CodecId != null)
			{
				try
				{
					codec = factory.GetOriginatorById<PolycomGroupSeriesDevice>(settings.CodecId.Value);
				}
				catch (KeyNotFoundException)
				{
					Logger.Log(eSeverity.Error, "No codec with id {0}", settings.CodecId);
				}
			}

			SetCodec(codec);
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(PolycomGroupSeriesCameraSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.CodecId = m_Codec == null ? null : (int?)m_Codec.Id;
			settings.CameraId = CameraId;
		}

		/// <summary>
		/// Override to add controls to the device.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		/// <param name="addControl"></param>
		protected override void AddControls(PolycomGroupSeriesCameraSettings settings, IDeviceFactory factory, Action<IDeviceControl> addControl)
		{
			base.AddControls(settings, factory, addControl);

			addControl(new GenericCameraRouteSourceControl<PolycomGroupSeriesCameraDevice>(this, 0));
			addControl(new CameraDeviceControl(this, 1));
			addControl(new PolycomGroupSeriesCameraDevicePowerControl(this, 2));
		}

		#endregion

		#region Codec Callbacks

		/// <summary>
		/// Subscribe to the codec events.
		/// </summary>
		/// <param name="codec"></param>
		private void Subscribe(PolycomGroupSeriesDevice codec)
		{
			if (codec == null)
				return;

			codec.OnIsOnlineStateChanged += CodecOnIsOnlineStateChanged;
		}

		/// <summary>
		/// Unsubscribe from the codec events.
		/// </summary>
		/// <param name="codec"></param>
		private void Unsubscribe(PolycomGroupSeriesDevice codec)
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
	}
}
