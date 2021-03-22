using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Cameras;
using ICD.Connect.Cameras.Controls;
using ICD.Connect.Cameras.Devices;
using ICD.Connect.Conferencing.Cisco.Devices.Codec;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Cameras;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Devices.EventArguments;
using ICD.Connect.Devices.Telemetry.DeviceInfo;
using ICD.Connect.Settings;

namespace ICD.Connect.Conferencing.Cisco.Devices.Camera
{
	public sealed class CiscoCodecCameraDevice : AbstractCameraDevice<CiscoCodecCameraDeviceSettings>
	{
		#region Events

		/// <summary>
		/// Raised when the parent codec changes.
		/// </summary>
		public event EventHandler OnCodecChanged;

        #endregion

		#region Fields

		private bool m_IsConnected;

		[CanBeNull]
		private CiscoCodecDevice m_Codec;

		[CanBeNull]
		private NearCamerasComponent m_CamerasComponent;

		[CanBeNull]
		private NearCamera m_Camera;

		private int? m_PanTiltSpeed;
		private int? m_ZoomSpeed;

		#endregion

		#region Properties

		[PublicAPI]
		public CiscoCodecDevice Codec { get { return m_Codec; } }

		/// <summary>
		/// Gets the ID for the specific camera being controlled on the wrapped Cisco SX unit.
		/// </summary>
		[PublicAPI]
		public int CameraId { get; private set; }

		[CanBeNull]
		public NearCamera Camera { get { return m_Camera; } }

		/// <summary>
		/// Gets the maximum number of presets this camera can support.
		/// </summary>
		public override int MaxPresets { get { return 35; } }

		private int PanSpeed { get { return m_PanTiltSpeed ?? (m_Camera == null ? 0 : m_Camera.PanSpeed); } }

		private int TiltSpeed { get { return m_PanTiltSpeed ?? (m_Camera == null ? 0 : m_Camera.TiltSpeed); } }

		private int ZoomSpeed { get { return m_ZoomSpeed ?? (m_Camera == null ? 0 : m_Camera.ZoomSpeed); } }

		#endregion

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			OnCodecChanged = null;

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
			Unsubscribe(m_Camera);

			m_Codec = codec;
			m_CamerasComponent = m_Codec == null ? null : m_Codec.Components.GetComponent<NearCamerasComponent>();
			m_Camera = m_CamerasComponent == null ? null : m_CamerasComponent.GetCamera(CameraId);

			Subscribe(m_Codec);
			Subscribe(m_CamerasComponent);
			Subscribe(m_Camera);

			Update(m_Camera);

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
			return m_Camera == null
				       ? Enumerable.Empty<CameraPreset>()
				       : m_Camera.GetPresets().Select(p => new CameraPreset(p.PresetId, p.Name));
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
				Logger.Log(eSeverity.Error, "Camera preset must be between 1 and {0}, preset was not activated.", MaxPresets);
				return;
			}

			m_Camera.ActivatePreset(presetId);
		}

		/// <summary>
		/// Stores the cameras current position with the given presetId.
		/// </summary>
		/// <param name="presetId"></param>
		public override void StorePreset(int presetId)
		{
			if (m_Camera == null)
				return;

			if (presetId < 1 || presetId > MaxPresets)
			{
				Logger.Log(eSeverity.Error, "Camera preset must be between 1 and {0}, preset was not stored.", MaxPresets);
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
			throw new NotSupportedException();
		}

		/// <summary>
		/// Resets camera to its predefined home position
		/// </summary>
		public override void ActivateHome()
		{
			if (m_Camera == null)
				return;

			m_Camera.Reset();
		}

		/// <summary>
		/// Stores the current position as the home position.
		/// </summary>
		public override void StoreHome()
		{
			if (m_Camera == null)
				return;

			// TODO - No way to store home position?
		}

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			return m_IsConnected && m_Codec != null && m_Codec.IsOnline;
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
			if (eventArgs.Data == CameraId)
				RaisePresetsChanged();
		}

		#endregion

		#region Near Camera  Callbacks

		private void Update(NearCamera camera)
		{
			m_IsConnected = camera != null && camera.Connected;

			string macAddress = camera == null ? string.Empty : camera.MacAddress ?? string.Empty;
			IcdPhysicalAddress mac;
			IcdPhysicalAddress.TryParse(macAddress, out mac);

			MonitoredDeviceInfo.Model = camera == null ? null : camera.Model;
			MonitoredDeviceInfo.SerialNumber = camera == null ? null : camera.SerialNumber;
			MonitoredDeviceInfo.FirmwareVersion = camera == null ? null : camera.SoftwareId;
			MonitoredDeviceInfo.NetworkInfo.Adapters.GetOrAddAdapter(1).MacAddress = mac;

		}

		private void Subscribe(NearCamera camera)
		{
			if (camera == null)
				return;

			camera.OnConnectedChanged += CameraOnConnectedChanged;
			camera.OnModelChanged += CameraOnModelChanged;
			camera.OnSerialNumberChanged += CameraOnSerialNumberChanged;
			camera.OnSoftwareIdChanged += CameraOnSoftwareIdChanged;
			camera.OnMacAddressChanged += CameraOnMacAddressChanged;
		}

		private void Unsubscribe(NearCamera camera)
		{
			if (camera == null)
				return;

			camera.OnConnectedChanged -= CameraOnConnectedChanged;
			camera.OnModelChanged -= CameraOnModelChanged;
			camera.OnSerialNumberChanged -= CameraOnSerialNumberChanged;
			camera.OnSoftwareIdChanged -= CameraOnSoftwareIdChanged;
			camera.OnMacAddressChanged -= CameraOnMacAddressChanged;
		}

		private void CameraOnConnectedChanged(object sender, BoolEventArgs e)
		{
			m_IsConnected = e.Data;
			UpdateCachedOnlineStatus();
		}

		private void CameraOnModelChanged(object sender, StringEventArgs e)
		{
			MonitoredDeviceInfo.Model = e.Data;
		}

		private void CameraOnSerialNumberChanged(object sender, StringEventArgs e)
		{
			MonitoredDeviceInfo.SerialNumber = e.Data;
		}

		private void CameraOnSoftwareIdChanged(object sender, StringEventArgs e)
		{
			MonitoredDeviceInfo.FirmwareVersion = e.Data;
		}

		private void CameraOnMacAddressChanged(object sender, StringEventArgs e)
		{
			string macAddress = e.Data ?? string.Empty;
			IcdPhysicalAddress mac;
			IcdPhysicalAddress.TryParse(macAddress, out mac);

			MonitoredDeviceInfo.NetworkInfo.Adapters.GetOrAddAdapter(1).MacAddress = mac;
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
				Logger.Log(eSeverity.Error, "No Camera Id");

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
					Logger.Log(eSeverity.Error, "No Cisco Codec Device with id {0}", settings.CodecId.Value);
				}
			}

			SetCodec(codec);

			SupportedCameraFeatures =
				m_Camera == null
					? eCameraFeatures.None
					: eCameraFeatures.PanTiltZoom |
					  eCameraFeatures.Presets |
					  eCameraFeatures.Home;

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

		/// <summary>
		/// Override to add controls to the device.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		/// <param name="addControl"></param>
		protected override void AddControls(CiscoCodecCameraDeviceSettings settings, IDeviceFactory factory, Action<IDeviceControl> addControl)
		{
			base.AddControls(settings, factory, addControl);

			addControl(new GenericCameraRouteSourceControl<CiscoCodecCameraDevice>(this, 0));
			addControl(new CameraDeviceControl(this, 1));
			addControl(new CiscoCodecCameraDevicePowerControl(this, 2));
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

		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<int>("SetPanSpeed", "SetPanSpeed <1-15>", i => Camera.PanSpeed = i);
			yield return new GenericConsoleCommand<int>("SetTiltSpeed", "SetTiltSpeed <1-15>", i => Camera.TiltSpeed = i);
			yield return new GenericConsoleCommand<int>("SetZoomSpeed", "SetZoomSpeed <1-15>", i => Camera.ZoomSpeed = i);
		}

		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
