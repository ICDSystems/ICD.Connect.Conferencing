using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Conferencing.Controls.Layout
{
	public static class ConferenceLayoutControlConsole
	{
		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static IEnumerable<IConsoleNodeBase> GetConsoleNodes(IConferenceLayoutControl instance)
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
		public static void BuildConsoleStatus(IConferenceLayoutControl instance, AddStatusRowDelegate addRow)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			addRow("Layout Available", instance.LayoutAvailable);
			addRow("Self-View Enabled", instance.SelfViewEnabled);
			addRow("Self-View FullScreen Enabled", instance.SelfViewFullScreenEnabled);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static IEnumerable<IConsoleCommand> GetConsoleCommands(IConferenceLayoutControl instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			yield return new GenericConsoleCommand<bool>("SetSelfViewEnabled", "SetSelfViewEnabled <true/false>", e => instance.SetSelfViewEnabled(e));
			yield return new GenericConsoleCommand<bool>("SetSelfViewFullScreenEnabled", "SetSelfViewFullScreenEnabled <true/false>", e => instance.SetSelfViewFullScreenEnabled(e));

			string layoutModeHelp = string.Format("SetLayoutMode <{0}>", StringUtils.ArrayFormat(EnumUtils.GetValues<eLayoutMode>()));
			yield return new GenericConsoleCommand<eLayoutMode>("SetLayoutMode", layoutModeHelp, m => instance.SetLayoutMode(m));
		}
	}
}
