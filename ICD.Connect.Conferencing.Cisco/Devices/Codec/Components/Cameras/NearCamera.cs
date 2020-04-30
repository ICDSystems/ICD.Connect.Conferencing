using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Xml;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Cameras;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Cameras
{
	public enum eCameraPan
	{
		[UsedImplicitly] Left,
		[UsedImplicitly] Right,
		[UsedImplicitly] Stop
	}

	public enum eCameraTilt
	{
		[UsedImplicitly] Down,
		[UsedImplicitly] Up,
		[UsedImplicitly] Stop
	}

	public enum eCameraZoom
	{
		[UsedImplicitly] In,
		[UsedImplicitly] Out,
		[UsedImplicitly] Stop
	}

	public enum eCameraFocus
	{
		[UsedImplicitly] Far,
		[UsedImplicitly] Near,
		[UsedImplicitly] Stop
	}

	/// <summary>
	/// NearCamera provides functionality for controlling a local camera.
	/// </summary>
	public sealed class NearCamera : AbstractCiscoCamera
	{
		private const int MIN_SPEED = 1;
		private const int MAX_SPEED = 15;

		private readonly NearCamerasComponent m_NearCamerasComponent;

		private int m_PanSpeed = 7;
		private int m_TiltSpeed = 7;
		private int m_ZoomSpeed = 7;

		private bool m_Connected;
		private string m_Model;
		private string m_SerialNumber;
		private string m_SoftwareId;
		private string m_MacAddress;

		#region Events

		/// <summary>
		/// Raised when the connected state changes
		/// </summary>
		public event EventHandler<BoolEventArgs> OnConnectedChanged;

		public event EventHandler<StringEventArgs> OnModelChanged;

		public event EventHandler<StringEventArgs> OnSerialNumberChanged;

		public event EventHandler<StringEventArgs> OnSoftwareIdChanged;

		public event EventHandler<StringEventArgs> OnMacAddressChanged;

		#endregion

		#region Properties

		/// <summary>
		/// Returns true if this camera is connected to the system.
		/// </summary>
		public bool Connected
		{
			get { return m_Connected; }
			private set
			{
				if (m_Connected == value)
					return;

				m_Connected = value;

				OnConnectedChanged.Raise(this, new BoolEventArgs(value));
			}
		}

		public string Model
		{
			get { return m_Model; }
			private set
			{
				if (m_Model == value)
					return;

				m_Model = value;

				OnModelChanged.Raise(this, new StringEventArgs(value));
			}
		}

		public string SerialNumber
		{
			get { return m_SerialNumber; }
			private set
			{
				if (m_SerialNumber == value)
					return;

				m_SerialNumber = value;

				OnSerialNumberChanged.Raise(this, new StringEventArgs(value));
			}
		}

		public string SoftwareId
		{
			get { return m_SoftwareId; }
			private set
			{
				if (m_SoftwareId == value)
					return;

				m_SoftwareId = value;

				OnSoftwareIdChanged.Raise(this, new StringEventArgs(value));
			}
		}

		public string MacAddress
		{
			get { return m_MacAddress; }
			private set
			{
				if (m_MacAddress == value)
					return;

				m_MacAddress = value;

				OnMacAddressChanged.Raise(this, new StringEventArgs(value));
			}
		}

		/// <summary>
		/// The id of the local camera.
		/// </summary>
		public int CameraId { get; private set; }

		/// <summary>
		/// Gets and sets the pan speed.
		/// </summary>
		[PublicAPI]
		public int PanSpeed { get { return m_PanSpeed; } set { m_PanSpeed = value; } }

		/// <summary>
		/// Gets and sets the tilt speed.
		/// </summary>
		[PublicAPI]
		public int TiltSpeed { get { return m_TiltSpeed; } set { m_TiltSpeed = value; } }

		/// <summary>
		/// Gets and sets the zoom speed.
		/// </summary>
		[PublicAPI]
		public int ZoomSpeed { get { return m_ZoomSpeed; } set { m_ZoomSpeed = value; } }

		#endregion

		#region Contructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="cameraId"></param>
		/// <param name="codec"></param>
		public NearCamera(int cameraId, CiscoCodecDevice codec)
			: base(codec)
		{
			m_NearCamerasComponent = codec.Components.GetComponent<NearCamerasComponent>();

			CameraId = cameraId;

			Subscribe(Codec);

			if (Codec.Initialized)
				Initialize();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Parses camera xml from the codec.
		/// </summary>
		/// <param name="xml"></param>
		public void Parse(string xml)
		{
			Connected = XmlUtils.TryReadChildElementContentAsBoolean(xml, "Connected") ?? false;
			Model = XmlUtils.TryReadChildElementContentAsString(xml, "Model") ?? string.Empty;
			SerialNumber = XmlUtils.TryReadChildElementContentAsString(xml, "SerialNumber") ?? string.Empty;
			SoftwareId = XmlUtils.TryReadChildElementContentAsString(xml, "SoftwareID") ?? string.Empty;
			MacAddress = XmlUtils.TryReadChildElementContentAsString(xml, "MacAddress") ?? string.Empty;
		}

		/// <summary>
		/// Pans the local camera.
		/// </summary>
		/// <param name="action"></param>
		[PublicAPI]
		public void Pan(eCameraPan action)
		{
			Pan(action, PanSpeed);
		}

		/// <summary>
		/// Pans the local camera.
		/// </summary>
		/// <param name="action"></param>
		/// <param name="speed"></param>
		[PublicAPI]
		public void Pan(eCameraPan action, int speed)
		{
			Codec.SendCommand("xCommand Camera Ramp CameraId: {0} Pan: {1} PanSpeed: {2}", CameraId, action, speed);
			Codec.Log(eSeverity.Informational, "Moving Camera {0} - Pan: {1} Speed: {2}", CameraId, action, speed);
		}

		/// <summary>
		/// Tilts the local camera.
		/// </summary>
		/// <param name="action"></param>
		[PublicAPI]
		public void Tilt(eCameraTilt action)
		{
			Tilt(action, TiltSpeed);
		}

		/// <summary>
		/// Tilts the local camera.
		/// </summary>
		/// <param name="action"></param>
		/// <param name="speed"></param>
		[PublicAPI]
		public void Tilt(eCameraTilt action, int speed)
		{
			Codec.SendCommand("xCommand Camera Ramp CameraId: {0} Tilt: {1} TiltSpeed: {2}", CameraId, action, speed);
			Codec.Log(eSeverity.Informational, "Moving Camera {0} - Tilt: {1} Speed: {2}", CameraId, action, speed);
		}

		/// <summary>
		/// Zooms the local camera.
		/// </summary>
		/// <param name="action"></param>
		[PublicAPI]
		public void Zoom(eCameraZoom action)
		{
			Zoom(action, ZoomSpeed);
		}

		/// <summary>
		/// Zooms the local camera.
		/// </summary>
		/// <param name="action"></param>
		/// <param name="speed"></param>
		[PublicAPI]
		public void Zoom(eCameraZoom action, int speed)
		{
			Codec.SendCommand("xCommand Camera Ramp CameraId: {0} Zoom: {1} ZoomSpeed: {2}", CameraId, action, speed);
			Codec.Log(eSeverity.Informational, "Moving Camera {0} - Zoom: {1} Speed: {2}", CameraId, action, speed);
		}

		/// <summary>
		/// Focuses the local camera.
		/// </summary>
		/// <param name="action"></param>
		[PublicAPI]
		public void Focus(eCameraFocus action)
		{
			Codec.SendCommand("xCommand Camera Ramp CameraId: {0} Focus: {1}", CameraId, action);
			Codec.Log(eSeverity.Informational, "Moving Camera {0} - Focus: {1}", CameraId, action);
		}

		/// <summary>
		/// Sets the camera to its default position.
		/// </summary>
		[PublicAPI]
		public void Default()
		{
			Codec.SendCommand("xCommand Camera Preset ActivateDefaultPosition CameraId: {0}", CameraId);
			Codec.Log(eSeverity.Informational, "Moving Camera {0} to default position", CameraId);
		}

		/// <summary>
		/// Resets the camera to its default position.
		/// </summary>
		[PublicAPI]
		public void Reset()
		{
			Codec.SendCommand("xCommand Camera PositionReset CameraId: {0}", CameraId);
			Codec.Log(eSeverity.Informational, "Resetting Camera {0}", CameraId);
		}

		/// <summary>
		/// Activates the preset at the given index for this camera.
		/// </summary>
		/// <param name="presetIndex"></param>
		[PublicAPI]
		public void ActivatePreset(int presetIndex)
		{
			m_NearCamerasComponent.ActivatePreset(CameraId, presetIndex);
		}

		/// <summary>
		/// Stores the current camera position as a preset with the given index.
		/// </summary>
		/// <param name="presetIndex"></param>
		[PublicAPI]
		public void StorePreset(int presetIndex)
		{
			m_NearCamerasComponent.StorePreset(CameraId, presetIndex);
		}

		/// <summary>
		/// Starts the camera moving.
		/// </summary>
		/// <param name="action"></param>
		public override void Pan(eCameraPanAction action)
		{
			switch (action)
			{
				case eCameraPanAction.Left:
					Pan(eCameraPan.Left);
					break;
				case eCameraPanAction.Right:
					Pan(eCameraPan.Right);
					break;
				case eCameraPanAction.Stop:
					StopPanTilt();
					break;
				default:
					throw new ArgumentOutOfRangeException("action");
			}
		}

		/// <summary>
		/// Starts the camera moving.
		/// </summary>
		/// <param name="action"></param>
		public override void Tilt(eCameraTiltAction action)
		{
			switch (action)
			{
				case eCameraTiltAction.Up:
					Tilt(eCameraTilt.Up);
					break;
				case eCameraTiltAction.Down:
					Tilt(eCameraTilt.Down);
					break;
				case eCameraTiltAction.Stop:
					StopPanTilt();
					break;
				default:
					throw new ArgumentOutOfRangeException("action");
			}
		}

		/// <summary>
		/// Stops all movement of the local camera.
		/// </summary>
		public override void StopPanTilt()
		{
			Codec.SendCommand("xCommand Camera Ramp CameraId: {0} Pan: {1} Tilt: {2}",
			                  CameraId, eCameraPan.Stop, eCameraTilt.Stop);
			Codec.Log(eSeverity.Informational, "Stopping Pan/Tilt on Camera {0}", CameraId);
		}

		/// <summary>
		/// Stops all movement of the local camera.
		/// </summary>
		public void StopZoom()
		{
			Codec.SendCommand("xCommand Camera Ramp CameraId: {0} Zoom: {1}",
							  CameraId, eCameraPan.Stop);
			Codec.Log(eSeverity.Informational, "Stopping Zoom on Camera {0}", CameraId);
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			string speedRange = StringUtils.RangeFormat(MIN_SPEED, MAX_SPEED);

			yield return new GenericConsoleCommand<int>("SetPanSpeed", "Set Pan Speed x " + speedRange, x => PanSpeed = x);
			yield return new GenericConsoleCommand<int>("SetTiltSpeed", "Set Tilt Speed x " + speedRange, x => TiltSpeed = x);
			yield return new GenericConsoleCommand<int>("SetZoomSpeed", "Set Zoom Speed x " + speedRange, x => ZoomSpeed = x);
			yield return new ConsoleCommand("Reset", "Resets the camera position", () => Reset());
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Connected", Connected);
			addRow("Camera Id", CameraId);
			addRow("Pan Speed", PanSpeed);
			addRow("Tilt Speed", TiltSpeed);
			addRow("Zoom Speed", ZoomSpeed);
		}

		/// <summary>
		/// Shim to avoid "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
