using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Camera
{
	public sealed class CameraComponent : AbstractPolycomComponent
	{
		private const int CAMERA_EXTENT = 50000;

		private static readonly BiDictionary<eCameraAction, string> s_ActionNames =
			new BiDictionary<eCameraAction, string>
			{
				{eCameraAction.Left, "left"},
				{eCameraAction.Right, "right"},
				{eCameraAction.Up, "up"},
				{eCameraAction.Down, "down"},
				{eCameraAction.ZoomIn, "zoom+"},
				{eCameraAction.ZoomOut, "zoom-"},
				{eCameraAction.Stop, "stop"}
			};

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="codec"></param>
		public CameraComponent(PolycomGroupSeriesDevice codec)
			: base(codec)
		{
			Subscribe(Codec);

			Codec.RegisterFeedback("notification", HandleNotification);

			if (Codec.Initialized)
				Initialize();
		}

		/// <summary>
		/// Called to initialize the component.
		/// </summary>
		protected override void Initialize()
		{
			base.Initialize();

			Codec.SendCommand("notify vidsourcechanges");
			Codec.SendCommand("preset register");
		}

		#region Methods

		/// <summary>
		/// Moves the near camera with the given action.
		/// </summary>
		/// <param name="action"></param>
		public void MoveNear(eCameraAction action)
		{
			string move = s_ActionNames.GetValue(action);

			Codec.SendCommand("camera near move {0}", move);
			Codec.Log(eSeverity.Informational, "Performing near camera move {0}", move);
		}

		/// <summary>
		/// Moves the far camera with the given action.
		/// </summary>
		/// <param name="action"></param>
		public void MoveFar(eCameraAction action)
		{
			string move = s_ActionNames.GetValue(action);

			Codec.SendCommand("camera far move {0}", move);
			Codec.Log(eSeverity.Informational, "Performing far camera move {0}", move);
		}

		/// <summary>
		/// Specifies a near camera as the main video source. 
		/// </summary>
		/// <param name="camera"></param>
		public void SetNearCameraAsVideoSource(int camera)
		{
			Codec.SendCommand("camera near {0}", camera);
			Codec.Log(eSeverity.Informational, "Setting near camera {0} as main video source", camera);
		}

		/// <summary>
		/// Specifies a far camera as the main video source. 
		/// </summary>
		/// <param name="camera"></param>
		public void SetFarCameraAsVideoSource(int camera)
		{
			Codec.SendCommand("camera far {0}", camera);
			Codec.Log(eSeverity.Informational, "Setting far camera {0} as main video source", camera);
		}

		/// <summary>
		/// Sets the position of the near camera.
		/// </summary>
		/// <param name="pan">-1 to 1</param>
		/// <param name="tilt">-1 to 1</param>
		/// <param name="zoom">-1 to 1</param>
		public void SetNearCameraPosition(float pan, float tilt, float zoom)
		{
			int panPosition = MathUtils.Clamp((int)(pan * CAMERA_EXTENT), -CAMERA_EXTENT, CAMERA_EXTENT);
			int tiltPosition = MathUtils.Clamp((int)(tilt * CAMERA_EXTENT), -CAMERA_EXTENT, CAMERA_EXTENT);
			int zoomPosition = MathUtils.Clamp((int)(zoom * CAMERA_EXTENT), -CAMERA_EXTENT, CAMERA_EXTENT);

			Codec.SendCommand("camera near setposition {0} {1} {2}", panPosition, tiltPosition, zoomPosition);
			Codec.Log(eSeverity.Informational, "Setting near camera position pan {0} tilt {1} zoom {2}", panPosition, tiltPosition, zoomPosition);
		}

		/// <summary>
		/// Sets the source for the specified camera to People.
		/// </summary>
		/// <param name="camera"></param>
		public void SetNearCameraForPeople(int camera)
		{
			Codec.SendCommand("camera for-people {0}", camera);
			Codec.Log(eSeverity.Informational, "Setting near camera {0} as for-people", camera);
		}

		/// <summary>
		/// Sets the source for the specified camera to People.
		/// </summary>
		/// <param name="camera"></param>
		public void SetNearCameraForContent(int camera)
		{
			Codec.SendCommand("camera for-content {0}", camera);
			Codec.Log(eSeverity.Informational, "Setting near camera {0} as for-content", camera);
		}

		/// <summary>
		/// Sets the video image of the near camera to upside down or normal.
		/// </summary>
		/// <param name="inverted"></param>
		public void SetNearCameraInverted(bool inverted)
		{
			Codec.SendCommand("camerainvert near {0}", inverted ? "on" : "off");
			Codec.Log(eSeverity.Informational, "Setting near camera invert {0}", inverted ? "on" : "off");
		}

		/// <summary>
		/// Applies the given preset to the near camera.
		/// </summary>
		/// <param name="preset"></param>
		public void GoNearCamreaPreset(int preset)
		{
			Codec.SendCommand("preset near go {0}", preset);
			Codec.Log(eSeverity.Informational, "Applying near camera preset {0}", preset);
		}

		/// <summary>
		/// Applies the given preset to the far camera.
		/// </summary>
		/// <param name="preset"></param>
		public void GoFarCameraPreset(int preset)
		{
			Codec.SendCommand("preset far go {0}", preset);
			Codec.Log(eSeverity.Informational, "Applying far camera preset {0}", preset);
		}

		/// <summary>
		/// Stores the near camera position as the given preset.
		/// </summary>
		/// <param name="preset"></param>
		public void SetNearCameraPreset(int preset)
		{
			Codec.SendCommand("preset near set {0}", preset);
			Codec.Log(eSeverity.Informational, "Storing near camera preset {0}", preset);
		}

		/// <summary>
		/// Stores the far camera position as the given preset.
		/// </summary>
		/// <param name="preset"></param>
		public void SetFarCameraPreset(int preset)
		{
			Codec.SendCommand("preset far set {0}", preset);
			Codec.Log(eSeverity.Informational, "Storing far camera preset {0}", preset);
		}

		#endregion

		/// <summary>
		/// Handles notification messages from the device.
		/// </summary>
		/// <param name="data"></param>
		private void HandleNotification(string data)
		{
			string[] split = data.Split(':');
			
			// notification:vidsourcechange:<near or far>:<camera index>:<cameraname>:<people or content>
			// notification:vidsourcechange:near:6:ppcip:content
			// notification:vidsourcechange:near:none:none:content
			if (split.Length != 6 || split[1] != "vidsourcechange")
				return;

			string location = split[2];
			string index = split[3];
			string name = split[4];
			string type = split[5];
		}

		#region Console

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			string moveValues = StringUtils.ArrayFormat(EnumUtils.GetValues<eCameraAction>());

			yield return new GenericConsoleCommand<eCameraAction>("MoveNear", string.Format("MoveNear <{0}>", moveValues), m => MoveNear(m));
			yield return new GenericConsoleCommand<eCameraAction>("MoveFar", string.Format("MoveFar <{0}>", moveValues), m => MoveFar(m));
			
			yield return new GenericConsoleCommand<int>("SetNearCameraAsVideoSource", "SetNearCameraAsVideoSource <CAMERA>", c => SetNearCameraAsVideoSource(c));
			yield return new GenericConsoleCommand<int>("SetFarCameraAsVideoSource", "SetFarCameraAsVideoSource <CAMERA>", c => SetFarCameraAsVideoSource(c));
			
			yield return new GenericConsoleCommand<float, float, float>("SetNearCameraPosition", "SetNearCameraPosition <PAN> <TILT> <ZOOM>", (x, y, z) => SetNearCameraPosition(x, y, z));
			
			yield return new GenericConsoleCommand<int>("SetNearCameraForPeople", "SetNearCameraForPeople <CAMERA>", c => SetNearCameraForPeople(c));
			yield return new GenericConsoleCommand<int>("SetNearCameraForContent", "SetNearCameraForContent <CAMERA>", c => SetNearCameraForContent(c));
			yield return new GenericConsoleCommand<bool>("SetNearCameraInverted", "SetNearCameraInverted <true/false>", i => SetNearCameraInverted(i));

			yield return new GenericConsoleCommand<int>("GoNearCamreaPreset", "GoNearCamreaPreset <PRESET>", p => GoNearCamreaPreset(p));
			yield return new GenericConsoleCommand<int>("GoFarCameraPreset", "GoFarCameraPreset <PRESET>", p => GoFarCameraPreset(p));
			yield return new GenericConsoleCommand<int>("SetNearCameraPreset", "SetNearCameraPreset <PRESET>", p => SetNearCameraPreset(p));
			yield return new GenericConsoleCommand<int>("SetFarCameraPreset", "SetFarCameraPreset <PRESET>", p => SetFarCameraPreset(p));
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
