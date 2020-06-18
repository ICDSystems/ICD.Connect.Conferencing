using System;
using System.Collections.Generic;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.DialContexts;

namespace ICD.Connect.Conferencing.Controls.Dialing
{
	public static class ConferenceDeviceControlConsole
	{
		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static IEnumerable<IConsoleNodeBase> GetConsoleNodes<T>(IConferenceDeviceControl<T> instance) where T : IConference
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			yield return
				ConsoleNodeGroup.IndexNodeMap("Conferences", "The conferences being tracked by this dialer",
				                              instance.GetConferences());
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="addRow"></param>
		public static void BuildConsoleStatus<T>(IConferenceDeviceControl<T> instance, AddStatusRowDelegate addRow) where T : IConference
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			addRow("AutoAnswer", instance.AutoAnswer);
			addRow("PrivacyMuted", instance.PrivacyMuted);
			addRow("DoNotDisturb", instance.DoNotDisturb);
			addRow("CameraMute", instance.CameraMute);
			addRow("Supports", instance.Supports);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static IEnumerable<IConsoleCommand> GetConsoleCommands<T>(IConferenceDeviceControl<T> instance) where T : IConference
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			yield return new GenericConsoleCommand<eDialProtocol, string>("Dial", "Dial <SIP/PSTN/Zoom> <NUMBER>", (p,s) => Dial(instance, p, s));
			yield return new GenericConsoleCommand<eDialProtocol, string, string>("PasswordDial", "PasswordDial <SIP/PSTN/Zoom> <NUMBER> <PASSWORD>", (p,s,t) => Dial(instance, p, s, t));
			yield return new GenericConsoleCommand<bool>("SetDoNotDisturb", "SetDoNotDisturb <true/false>", b => instance.SetDoNotDisturb(b));
			yield return new GenericConsoleCommand<bool>("SetAutoAnswer", "SetAutoAnswer <true/false>", b => instance.SetAutoAnswer(b));
			yield return new GenericConsoleCommand<bool>("SetPrivacyMute", "SetPrivacyMute <true/false>", b => instance.SetPrivacyMute(b));
			yield return new GenericConsoleCommand<bool>("SetCameraMute", "SetCameraMute <true/false>", b => instance.SetCameraMute(b));
		}

		private static void Dial<T>(IConferenceDeviceControl<T> instance, eDialProtocol protocol, string number) where T : IConference
		{
			DialContext context =
				new DialContext
				{
					Protocol = protocol,
					DialString = number
				};

			instance.Dial(context);
		}

		private static void Dial<T>(IConferenceDeviceControl<T> instance, eDialProtocol protocol, string number, string password) where T : IConference
		{
			DialContext context =
				new DialContext
				{
					Protocol = protocol,
					DialString = number,
					Password = password
				};

			instance.Dial(context);
		}
	}
}
