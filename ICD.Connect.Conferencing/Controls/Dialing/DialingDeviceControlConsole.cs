﻿using System;
using System.Collections.Generic;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.DialContexts;

namespace ICD.Connect.Conferencing.Controls.Dialing
{
	public static class DialingDeviceControlConsole
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
			yield return new GenericConsoleCommand<bool>("SetDoNotDisturb", "SetDoNotDisturb <true/false>", b => instance.SetDoNotDisturb(b));
			yield return new GenericConsoleCommand<bool>("SetAutoAnswer", "SetAutoAnswer <true/false>", b => instance.SetAutoAnswer(b));
			yield return new GenericConsoleCommand<bool>("SetPrivacyMute", "SetPrivacyMute <true/false>", b => instance.SetPrivacyMute(b));
		}

		private static void Dial<T>(IConferenceDeviceControl<T> instance, eDialProtocol protocol, string number) where T : IConference
		{
			switch (protocol)
			{
				case eDialProtocol.Pstn:
					instance.Dial(new PstnDialContext { DialString = number });
					break;
				case eDialProtocol.Sip:
					instance.Dial(new SipDialContext { DialString = number });
					break;
				case eDialProtocol.Zoom:
					instance.Dial(new ZoomDialContext{ DialString = number });
					break;
				case eDialProtocol.ZoomContact:
					instance.Dial(new ZoomContactDialContext { DialString = number });
					break;
			}
		}
	}
}
