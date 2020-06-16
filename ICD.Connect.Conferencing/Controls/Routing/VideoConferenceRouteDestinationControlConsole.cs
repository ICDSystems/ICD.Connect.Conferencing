using System;
using System.Collections.Generic;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Conferencing.Controls.Routing
{
	public static class VideoConferenceRouteDestinationControlConsole
	{
		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static IEnumerable<IConsoleNodeBase> GetConsoleNodes(IVideoConferenceRouteControl instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			yield break;
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="addRow"></param>
		public static void BuildConsoleStatus(IVideoConferenceRouteControl instance, AddStatusRowDelegate addRow)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			addRow("Camera Input", instance.CameraInput);
			addRow("Content Input", instance.ContentInput);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static IEnumerable<IConsoleCommand> GetConsoleCommands(IVideoConferenceRouteControl instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			yield return new GenericConsoleCommand<int, int>("SetCameraInput", "SetCameraInput <INPUT> <CAMERA ID>", (a, b) => instance.SetCameraInput(a, b));
			yield return new GenericConsoleCommand<int, int>("SetContentInput", "SetContentInput <INPUT> <CONTENT ID>", (a, b) => instance.SetContentInput(a, b));
		}
	}
}
