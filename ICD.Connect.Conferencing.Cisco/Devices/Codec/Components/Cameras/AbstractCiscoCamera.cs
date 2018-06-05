using System.Collections.Generic;
using ICD.Connect.API.Commands;
using ICD.Connect.Cameras;
using ICD.Connect.Conferencing.Cisco.Devices.Codec;

namespace ICD.Connect.Conferencing.Cisco.Components.Cameras
{
	/// <summary>
	/// Base class for cameras.
	/// </summary>
	public abstract class AbstractCiscoCamera : AbstractCiscoComponent
	{
		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="codec"></param>
		protected AbstractCiscoCamera(CiscoCodecDevice codec) : base(codec)
		{
		}

		#endregion

		#region Methods

		/// <summary>
		/// Starts the camera moving.
		/// </summary>
		/// <param name="action"></param>
		public abstract void PanTilt(eCameraPanTiltAction action);

		/// <summary>
		/// Stops the camera from moving.
		/// </summary>
		public abstract void StopPanTilt();

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new EnumConsoleCommand<eCameraPanTiltAction>("PanTilt", e => PanTilt(e));
			yield return new ConsoleCommand("StopPanTilt", "Stops moving the camera", () => StopPanTilt());
		}

		#endregion

		/// <summary>
		/// Shim to get around "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}
	}
}
