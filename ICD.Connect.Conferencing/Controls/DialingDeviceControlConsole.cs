using System;
using System.Collections.Generic;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Conferencing.Controls
{
	public static class DialingDeviceControlConsole
	{
		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static IEnumerable<IConsoleNodeBase> GetConsoleNodes(IDialingDeviceControl instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			yield return
				ConsoleNodeGroup.IndexNodeMap("Sources", "The conference sources being tracked by this dialer",
				                              instance.GetSources());
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="addRow"></param>
		public static void BuildConsoleStatus(IDialingDeviceControl instance, AddStatusRowDelegate addRow)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			addRow("AutoAnswer", instance.AutoAnswer);
			addRow("PrivacyMuted", instance.PrivacyMuted);
			addRow("DoNotDisturb", instance.DoNotDisturb);
			addRow("Supports", instance.Supports);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static IEnumerable<IConsoleCommand> GetConsoleCommands(IDialingDeviceControl instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			yield return new GenericConsoleCommand<string>("Dial", "Dial <NUMBER>", s => instance.Dial(s));
			yield return new GenericConsoleCommand<bool>("SetDoNotDisturb", "SetDoNotDisturb <true/false>", b => instance.SetDoNotDisturb(b));
			yield return new GenericConsoleCommand<bool>("SetAutoAnswer", "SetAutoAnswer <true/false>", b => instance.SetAutoAnswer(b));
			yield return new GenericConsoleCommand<bool>("SetPrivacyMute", "SetPrivacyMute <true/false>", b => instance.SetPrivacyMute(b));
		}
	}
}
