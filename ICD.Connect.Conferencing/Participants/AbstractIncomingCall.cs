using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.Participants
{
	public delegate void IncomingCallAnswerCallback(IIncomingCall sender);

	public delegate void IncomingCallRejectCallback(IIncomingCall sender);

	public abstract class AbstractIncomingCall : IIncomingCall, IDisposable
	{
		private string m_Name;
		private string m_Number;
		private eCallAnswerState m_AnswerState;
		private eCallDirection m_Direction;

		protected AbstractIncomingCall()
		{
			Direction = eCallDirection.Incoming;
		}

		public IncomingCallAnswerCallback AnswerCallback { get; set; }
		public IncomingCallRejectCallback RejectCallback { get; set; }

		/// <summary>
		/// Gets the source name.
		/// </summary>
		public string Name
		{
			get { return m_Name; }
			set
			{
				if (value == m_Name)
					return;

				m_Name = value;

				Log(eSeverity.Informational, "Name set to {0}", m_Name);

				OnNameChanged.Raise(this, new StringEventArgs(m_Name));
			}
		}

		/// <summary>
		/// Gets the source number.
		/// </summary>
		public string Number
		{
			get { return m_Number; }
			set
			{
				if (value == m_Number)
					return;

				m_Number = value;

				Log(eSeverity.Informational, "Number set to {0}", m_Number);

				OnNumberChanged.Raise(this, new StringEventArgs(m_Number));
			}
		}

		/// <summary>
		/// Source direction (Incoming, Outgoing, etc)
		/// </summary>
		public eCallDirection Direction
		{
			get { return m_Direction; }
			set
			{
				if (value == m_Direction)
					return;

				m_Direction = value;

				Log(eSeverity.Informational, "Direction set to {0}", m_Direction);
			}
		}

		/// <summary>
		/// Source Answer State (Ignored, Answered, etc)
		/// </summary>
		public eCallAnswerState AnswerState
		{
			get { return m_AnswerState; }
			set
			{
				if (value == m_AnswerState)
					return;

				m_AnswerState = value;

				Log(eSeverity.Informational, "AnswerState set to {0}", m_AnswerState);

				OnAnswerStateChanged.Raise(this, new IncomingCallAnswerStateEventArgs(m_AnswerState));
			}
		}

		/// <summary>
		/// Gets the name of the node.
		/// </summary>
		public string ConsoleName { get { return "IncomingCall"; } }

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public string ConsoleHelp { get { return string.Empty; } }

		public virtual event EventHandler<StringEventArgs> OnNameChanged;
		public virtual event EventHandler<StringEventArgs> OnNumberChanged;
		public virtual event EventHandler<IncomingCallAnswerStateEventArgs> OnAnswerStateChanged;

		public void Dispose()
		{
			AnswerCallback = null;
			RejectCallback = null;
		}

		/// <summary>
		/// Answers the incoming source.
		/// </summary>
		public void Answer()
		{
			IncomingCallAnswerCallback handler = AnswerCallback;
			if (handler != null)
				handler(this);
		}

		/// <summary>
		/// Rejects the incoming source.
		/// </summary>
		public void Reject()
		{
			IncomingCallRejectCallback handler = RejectCallback;
			if (handler != null)
				handler(this);
		}

		private void Log(eSeverity severity, string message, params object[] args)
		{
			message = string.Format("{0} - {1}", this, message);
			ServiceProvider.GetService<ILoggerService>().AddEntry(severity, message, args);
		}

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			yield break;
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			addRow("Name", Name);
			addRow("Number", Number);
			addRow("Direction", Direction);
			addRow("AnswerState", AnswerState);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield return new ConsoleCommand("Answer", "Answers the incoming call", () => Answer());
			yield return new ConsoleCommand("Reject", "Rejects the incoming call", () => Reject());
		}
	}
}
