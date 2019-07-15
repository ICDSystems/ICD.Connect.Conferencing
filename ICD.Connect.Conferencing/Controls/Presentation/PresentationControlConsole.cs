using System;
using System.Collections.Generic;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Conferencing.Controls.Presentation
{
	public static class PresentationControlConsole
	{
		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static IEnumerable<IConsoleNodeBase> GetConsoleNodes(IPresentationControl instance)
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
		public static void BuildConsoleStatus(IPresentationControl instance, AddStatusRowDelegate addRow)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			addRow("Presentation Active Input", instance.PresentationActiveInput);
			addRow("Presentation Active", instance.PresentationActive);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static IEnumerable<IConsoleCommand> GetConsoleCommands(IPresentationControl instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");
			
			yield return new GenericConsoleCommand<int>("StartPresentation", "StartPresentation <INPUT>", i => instance.StartPresentation(i));
			yield return new ConsoleCommand("StopPresentation", "Stops the active presentation", () => instance.StopPresentation());
		}
	}
}
