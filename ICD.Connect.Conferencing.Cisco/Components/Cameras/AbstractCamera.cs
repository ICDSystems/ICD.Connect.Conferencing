using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Connect.API.Commands;
using ICD.Connect.Cameras;
using ICD.Connect.Conferencing.Cameras;

namespace ICD.Connect.Conferencing.Cisco.Components.Cameras
{
	/// <summary>
	/// Base class for cameras.
	/// </summary>
	public abstract class AbstractCamera : AbstractCiscoComponent, ICiscoCamera
	{
		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="codec"></param>
		protected AbstractCamera(CiscoCodec codec) : base(codec)
		{
		}

		#endregion

		#region Methods

		/// <summary>
		/// Starts the camera moving.
		/// </summary>
		/// <param name="action"></param>
		public abstract void Move(eCameraPanTiltAction action);

		/// <summary>
		/// Stops the camera from moving.
		/// </summary>
		public abstract void Stop();

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			string moveParams = StringUtils.ArrayFormat(EnumUtils.GetValues<eCameraPanTiltAction>());
			yield return new GenericConsoleCommand<eCameraPanTiltAction>("Move", "Move x " + moveParams, e => Move(e));
			yield return new ConsoleCommand("Stop", "Stops moving the camera", () => Stop());
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
