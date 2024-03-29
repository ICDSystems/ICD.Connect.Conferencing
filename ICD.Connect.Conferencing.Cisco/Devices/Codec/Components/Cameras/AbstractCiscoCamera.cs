﻿using System.Collections.Generic;
using ICD.Connect.API.Commands;
using ICD.Connect.Cameras;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Cameras
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
		protected AbstractCiscoCamera(CiscoCodecDevice codec)
			: base(codec)
		{
		}

		#endregion

		#region Methods

		/// <summary>
		/// Starts the camera moving.
		/// </summary>
		/// <param name="action"></param>
		public abstract void Pan(eCameraPanAction action);

		/// <summary>
		/// Starts the camera moving
		/// </summary>
		/// <param name="action"></param>
		public abstract void Tilt(eCameraTiltAction action);

		/// <summary>
		/// Stops the camera from moving.
		/// </summary>
		public abstract void StopPanTilt();

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

			yield return new EnumConsoleCommand<eCameraPanAction>("Pan", e => Pan(e));
			yield return new EnumConsoleCommand<eCameraTiltAction>("Tilt", e => Tilt(e));
			yield return new ConsoleCommand("StopPanTilt", "Stops moving the camera", () => StopPanTilt());
		}

		/// <summary>
		/// Shim to get around "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
