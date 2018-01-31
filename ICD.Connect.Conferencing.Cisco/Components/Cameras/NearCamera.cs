using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Xml;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Cameras;
using ICD.Connect.Conferencing.Cameras;

namespace ICD.Connect.Conferencing.Cisco.Components.Cameras
{


	/// <summary>
	/// NearCamera provides functionality for controlling a local camera.
	/// </summary>
	public sealed class NearCamera : AbstractCamera
	{
		private const int MIN_SPEED = 1;
		private const int MAX_SPEED = 15;

		private int m_PanSpeed = 7;
		private int m_TiltSpeed = 7;
		private int m_ZoomSpeed = 7;

		#region Properties

		/// <summary>
		/// Returns true if this camera is connected to the system.
		/// </summary>
		public bool Connected { get; private set; }

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
		public NearCamera(int cameraId, CiscoCodec codec) : base(codec)
		{
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
			Connected = XmlUtils.TryReadChildElementContentAsBoolean(xml, "Connected") ?? Connected;
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
		/// Activates the preset with the given id.
		/// </summary>
		/// <param name="presetId"></param>
		[PublicAPI]
		public void ActivatePreset(int presetId)
		{
			Codec.SendCommand("xCommand Camera Preset Activate PresetId: {0}", presetId);
			Codec.Log(eSeverity.Informational, "Activating Camera {0} Preset {1}", CameraId, presetId);
		}

		/// <summary>
		/// Stores the current camera position as a preset with the given name.
		/// </summary>
		/// <param name="presetId"></param>
		[PublicAPI]
		public void StorePreset(int presetId)
		{
			Codec.SendCommand("xCommand Camera Preset Store CameraId: {0} PresetId: \"{1}\"", CameraId, presetId);
			Codec.Log(eSeverity.Informational, "Storing preset {0} for Camera {1}", presetId, CameraId);

			// Updates the presets
			Codec.SendCommand("xCommand Camera Preset List");
		}

		/// <summary>
		/// Starts the camera moving.
		/// </summary>
		/// <param name="action"></param>
		public override void Move(eCameraPanTiltAction action)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Stops all movement of the local camera.
		/// </summary>
		public override void Stop()
		{
			Codec.SendCommand("xCommand Camera Ramp CameraId: {0} Pan: {1} Tilt: {2} Zoom: {3} Focus: {4}",
			                  CameraId, eCameraPan.Stop, eCameraTilt.Stop, eCameraZoom.Stop, eCameraFocus.Stop);
			Codec.Log(eSeverity.Informational, "Stopping Camera {0}", CameraId);
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
// Ignore missing comment warning.
#pragma warning disable 1591
	public enum eCameraPan
	{
		[UsedImplicitly]
		Left,
		[UsedImplicitly]
		Right,
		[UsedImplicitly]
		Stop
	}

	public enum eCameraTilt
	{
		[UsedImplicitly]
		Down,
		[UsedImplicitly]
		Up,
		[UsedImplicitly]
		Stop
	}

	public enum eCameraZoom
	{
		[UsedImplicitly]
		In,
		[UsedImplicitly]
		Out,
		[UsedImplicitly]
		Stop
	}

	public enum eCameraFocus
	{
		[UsedImplicitly]
		Far,
		[UsedImplicitly]
		Near,
		[UsedImplicitly]
		Stop
	}
#pragma warning restore 1591
}
