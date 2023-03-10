using System.Collections.Generic;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.Cameras;
using ICD.Connect.Conferencing.Cameras;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Cameras
{
	/// <summary>
	/// FarCamera provides functionality for controlling a remote camera.
	/// </summary>
	public sealed class FarCamera : AbstractCiscoCamera, IRemoteCamera
	{
		/// <summary>
		/// Mapping of eCameraPanAction to cisco command text
		/// </summary>
		private static readonly Dictionary<eCameraPanAction, string> s_PanActionToCisco = new Dictionary
			<eCameraPanAction, string>
		{
			{eCameraPanAction.Left, "Left"},
			{eCameraPanAction.Right, "Right"}
		};

		/// <summary>
		/// Mapping of eCameraTiltAction to cisco command text
		/// </summary>
		private static readonly Dictionary<eCameraTiltAction, string> s_TiltActionToCisco = new Dictionary
			<eCameraTiltAction, string>
		{
			{eCameraTiltAction.Up, "Up"},
			{eCameraTiltAction.Down, "Down"}
		};

		/// <summary>
		/// Mapping of eCameraZoomAction to command text
		/// </summary>
		private static readonly Dictionary<eCameraZoomAction, string> s_ZoomActionToCisco = new Dictionary
			<eCameraZoomAction, string>
		{
			{eCameraZoomAction.ZoomIn, "ZoomIn"},
			{eCameraZoomAction.ZoomOut, "ZoomOut"}
		};
		
		/// <summary>
		/// The CallId for the remote camera.
		/// </summary>
		private readonly int m_CallId;

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public override string ConsoleHelp { get { return "Far camera for call id: " + m_CallId; } }

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="callId"></param>
		/// <param name="codec"></param>
		public FarCamera(int callId, CiscoCodecDevice codec) : base(codec)
		{
			m_CallId = callId;

			Subscribe(Codec);

			if (Codec.Initialized)
				Initialize();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Moves the camera.
		/// </summary>
		/// <param name="action"></param>
		public override void Pan(eCameraPanAction action)
		{
			if (action == eCameraPanAction.Stop)
			{
				StopPanTilt();
				return;
			}

			SendMoveCommand(s_PanActionToCisco[action]);
			Codec.Logger.Log(eSeverity.Informational, "Moving Far End Camera CallId: {0}, Direction: {1}", m_CallId, action);
		}

		/// <summary>
		/// Moves the camera.
		/// </summary>
		/// <param name="action"></param>
		public override void Tilt(eCameraTiltAction action)
		{
			if (action == eCameraTiltAction.Stop)
			{
				StopPanTilt();
				return;
			}

			SendMoveCommand(s_TiltActionToCisco[action]);
			Codec.Logger.Log(eSeverity.Informational, "Moving Far End Camera CallId: {0}, Direction: {1}", m_CallId, action);
		}

		/// <summary>
		/// Stops the camera from moving.
		/// </summary>
		public override void StopPanTilt()
		{
			StopMove();
		}

		/// <summary>
		/// Zooms the camera
		/// </summary>
		/// <param name="action"></param>
		public void Zoom(eCameraZoomAction action)
		{
			if (action == eCameraZoomAction.Stop)
			{
				StopZoom();
				return;
			}

			SendMoveCommand(s_ZoomActionToCisco[action]);
			Codec.Logger.Log(eSeverity.Informational, "Zooming Far End Camera CallId: {0}, Direction: {1}", m_CallId, action);
		}

		/// <summary>
		/// Stops the camera from zooming
		/// </summary>
		public void StopZoom()
		{
			StopMove();
		}

		private void SendMoveCommand(string command)
		{
			Codec.SendCommand("xCommand Call FarEndControl Camera Move CallId: {0} Value: {1}", m_CallId, command);
		}

		private void StopMove()
		{
			Codec.SendCommand("xCommand Call FarEndControl Camera Stop CallId: {0}", m_CallId);
			Codec.Logger.Log(eSeverity.Informational, "Stop Moving Far End Camera CallId: {0}", m_CallId);
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

			yield return new EnumConsoleCommand<eCameraZoomAction>("Zoom", e => Zoom(e));
			yield return new ConsoleCommand("StopZoom", "Stops moving the camera", () => StopZoom());
		}

		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
