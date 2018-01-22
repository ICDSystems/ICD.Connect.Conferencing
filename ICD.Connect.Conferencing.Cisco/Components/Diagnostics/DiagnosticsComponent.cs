using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Xml;
using ICD.Connect.API.Commands;

namespace ICD.Connect.Conferencing.Cisco.Components.Diagnostics
{
	/// <summary>
	/// The DiagnosticsComponent provides status information about the codec itself.
	/// </summary>
	public sealed class DiagnosticsComponent : AbstractCiscoComponent
	{
		private readonly IcdHashSet<DiagnosticsMessage> m_Messages;
		private readonly SafeCriticalSection m_MessagesSection;

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="codec"></param>
		public DiagnosticsComponent(CiscoCodec codec)
			: base(codec)
		{
			m_Messages = new IcdHashSet<DiagnosticsMessage>();
			m_MessagesSection = new SafeCriticalSection();

			Subscribe(Codec);

			if (Codec.Initialized)
				Initialize();
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Subscribes to the codec events.
		/// </summary>
		/// <param name="codec"></param>
		protected override void Subscribe(CiscoCodec codec)
		{
			base.Subscribe(codec);

			if (codec == null)
				return;

			codec.RegisterParserCallback(ParseMessages, CiscoCodec.XSTATUS_ELEMENT, "Diagnostics");
		}

		/// <summary>
		/// Unsubscribes from the codec events.
		/// </summary>
		/// <param name="codec"></param>
		protected override void Unsubscribe(CiscoCodec codec)
		{
			base.Unsubscribe(codec);

			if (codec == null)
				return;

			codec.UnregisterParserCallback(ParseMessages, CiscoCodec.XSTATUS_ELEMENT, "Diagnostics");
		}

		/// <summary>
		/// Called when diagnostics messages are received.
		/// </summary>
		/// <param name="codec"></param>
		/// <param name="resultid"></param>
		/// <param name="xml"></param>
		private void ParseMessages(CiscoCodec codec, string resultid, string xml)
		{
			m_MessagesSection.Enter();

			try
			{
				IcdHashSet<DiagnosticsMessage> oldMessages = new IcdHashSet<DiagnosticsMessage>(m_Messages);

				using (IcdXmlReader reader = new IcdXmlReader(xml))
				{
					reader.ReadToNextElement();

					foreach (string child in reader.GetChildElementsAsString())
					{
						DiagnosticsMessage message = DiagnosticsMessage.FromXml(child);
						// Remove the message if the issue is resolved
						if (message.Level == DiagnosticsMessage.eLevel.Ok)
						{
							m_Messages.Remove(m_Messages.FirstOrDefault(message.IsSameIssue));
						}
						else
						{
							DiagnosticsMessage sameIssueMessage = m_Messages.FirstOrDefault(message.IsSameIssue);
							// Add the message if it is new
							if (sameIssueMessage == default(DiagnosticsMessage))
							{
								m_Messages.Add(message);
							}
							// Replace the old message if it has changed
							else if (sameIssueMessage != message)
							{
								m_Messages.Remove(sameIssueMessage);
								m_Messages.Add(message);
							}
						}
					}
				}

				// Log the changed messages
				IEnumerable<DiagnosticsMessage> delta = GetDelta(oldMessages, m_Messages);
				foreach (DiagnosticsMessage message in delta)
					Log(message);
			}
			finally
			{
				m_MessagesSection.Leave();
			}
		}

		/// <summary>
		/// Returns the messages that have changed state between the two sets.
		/// </summary>
		/// <param name="oldMessages"></param>
		/// <param name="newMessages"></param>
		/// <returns></returns>
		private static IEnumerable<DiagnosticsMessage> GetDelta(IcdHashSet<DiagnosticsMessage> oldMessages,
		                                                        IcdHashSet<DiagnosticsMessage> newMessages)
		{
			IcdHashSet<DiagnosticsMessage> output = new IcdHashSet<DiagnosticsMessage>();

			// Check old issues for new state
			foreach (DiagnosticsMessage message in oldMessages)
			{
				DiagnosticsMessage newer = newMessages.FirstOrDefault(message.IsSameIssue);

				// Issue was resolved
				if (newer == default(DiagnosticsMessage))
					output.Add(DiagnosticsMessage.GetResolved(message));
					// Issue changed
				else if (newer != message)
					output.Add(newer);
			}

			// Search for new issues
			foreach (DiagnosticsMessage message in newMessages)
			{
				DiagnosticsMessage older = oldMessages.FirstOrDefault(message.IsSameIssue);

				// Issue is new
				if (older == default(DiagnosticsMessage))
					output.Add(message);
			}

			return output;
		}

		/// <summary>
		/// Logs the message with the codec.
		/// </summary>
		/// <param name="message"></param>
		private void Log(DiagnosticsMessage message)
		{
			if (message.Level == DiagnosticsMessage.eLevel.Ok)
				return;

			eSeverity severity = GetSeverity(message.Level);
			Codec.Log(severity, "{0} - {1} ({2})", message.Type, message.Description, message.References);
		}

		/// <summary>
		/// Converts message level to logging severity.
		/// </summary>
		/// <param name="level"></param>
		/// <returns></returns>
		private static eSeverity GetSeverity(DiagnosticsMessage.eLevel level)
		{
			switch (level)
			{
				case DiagnosticsMessage.eLevel.Warning:
					return eSeverity.Warning;

				case DiagnosticsMessage.eLevel.Error:
					return eSeverity.Error;

				case DiagnosticsMessage.eLevel.Critical:
					return eSeverity.Critical;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// Prints out all the diagnostics messages currently in the set to the console
		/// </summary>
		private string PrintMessages()
		{
			TableBuilder builder = new TableBuilder("Type", "Level", "Description", "References");
			m_MessagesSection.Enter();

			try
			{
				foreach (DiagnosticsMessage message in m_Messages)
					builder.AddRow(message.Type, message.Level, message.Description, message.References);
			}
			finally
			{
				m_MessagesSection.Leave();
			}
			
			return builder.ToString();
		}

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

			yield return
				new ConsoleCommand("PrintMessages", "Prints out all diagnostics messages currently in the list", () => PrintMessages());
		}

		/// <summary>
		/// Shim to avoid "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
