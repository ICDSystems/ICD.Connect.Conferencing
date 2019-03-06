using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Xml;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.RoomAnalytics
{
	public sealed class RoomAnalyticsComponent : AbstractCiscoComponent
	{
		/// <summary>
		/// Raised when PeopleCount out-of-call is enabled/disabled.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnPeopleCountOutOfCallEnabledChanged;

		private bool m_PeopleCountOutOfCallEnabled;

		public bool PeopleCountOutOfCallEnabled
		{
			get { return m_PeopleCountOutOfCallEnabled; }
			private set
			{
				if (value == m_PeopleCountOutOfCallEnabled)
					return;
				
				m_PeopleCountOutOfCallEnabled = value;

				OnPeopleCountOutOfCallEnabledChanged.Raise(this, new BoolEventArgs(m_PeopleCountOutOfCallEnabled));
			}
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="codec"></param>
		public RoomAnalyticsComponent(CiscoCodecDevice codec)
			: base(codec)
		{
			Subscribe(Codec);

			if (Codec.Initialized)
				Initialize();
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			OnPeopleCountOutOfCallEnabledChanged = null;

			base.Dispose(disposing);
		}

		/// <summary>
		/// Enables/disables PeopleCount while not in a call.
		/// </summary>
		/// <param name="enable"></param>
		public void EnablePeopleCountOutOfCall(bool enable)
		{
			string value = enable ? "On" : "Off";

			Codec.SendCommand("xConfiguration RoomAnalytics PeopleCountOutOfCall: {0}", value);
			Codec.Log(eSeverity.Informational, "Setting RoomAnalytics PeopleCountOutOfCall {0}", value);
		}

		#region Codec Feedback

		/// <summary>
		/// Subscribes to the codec events.
		/// </summary>
		/// <param name="codec"></param>
		protected override void Subscribe(CiscoCodecDevice codec)
		{
			base.Subscribe(codec);

			if (codec == null)
				return;

			codec.RegisterParserCallback(ParsePeopleCountOutOfCall, CiscoCodecDevice.XCONFIGURATION_ELEMENT, "RoomAnalytics", "PeopleOutOfCall");
		}

		/// <summary>
		/// Unsubscribes from the codec events.
		/// </summary>
		/// <param name="codec"></param>
		protected override void Unsubscribe(CiscoCodecDevice codec)
		{
			base.Unsubscribe(codec);

			if (codec == null)
				return;

			codec.UnregisterParserCallback(ParsePeopleCountOutOfCall, CiscoCodecDevice.XCONFIGURATION_ELEMENT, "RoomAnalytics", "PeopleOutOfCall");
		}

		private void ParsePeopleCountOutOfCall(CiscoCodecDevice codec, string resultid, string xml)
		{
			string content = XmlUtils.GetInnerXml(xml);
			PeopleCountOutOfCallEnabled = string.Equals(content, "On", StringComparison.OrdinalIgnoreCase);
		}

		#endregion

		#region Console

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("PeopleCount OutOfCall Enabled", PeopleCountOutOfCallEnabled);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<bool>("EnablePeopleCountOutOfCall", "EnablePeopleCountOutOfCall <true/false>",
			                                             b => EnablePeopleCountOutOfCall(b));
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
