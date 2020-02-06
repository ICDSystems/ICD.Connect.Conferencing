using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Conferencing.Devices
{
	public static class VideoConferenceDeviceConsole
	{
		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static IEnumerable<IConsoleNodeBase> GetConsoleNodes(IVideoConferenceDevice instance)
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
		public static void BuildConsoleStatus(IVideoConferenceDevice instance, AddStatusRowDelegate addRow)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			addRow("Default Camera", instance.DefaultCamera);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static IEnumerable<IConsoleCommand> GetConsoleCommands(IVideoConferenceDevice instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			yield return new ConsoleCommand("PrintInputTypes", "Prints a table of the configured input types", () => PrintInputTypes(instance));
		}

		private static string PrintInputTypes(IVideoConferenceDevice instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			TableBuilder builder = new TableBuilder("Input", "Type");

			foreach (KeyValuePair<int, eCodecInputType> kvp in instance.InputTypes.GetInputs())
				builder.AddRow(kvp.Key, kvp.Value);

			return builder.ToString();
		}
	}
}
