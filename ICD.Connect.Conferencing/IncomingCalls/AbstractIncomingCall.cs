using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants.Enums;

namespace ICD.Connect.Conferencing.IncomingCalls
{
	public delegate void IncomingCallAnswerCallback(IIncomingCall sender);

	public delegate void IncomingCallRejectCallback(IIncomingCall sender);

	public abstract class AbstractIncomingCall : IIncomingCall, IDisposable
	{
		public event EventHandler<StringEventArgs> OnNameChanged;
		public event EventHandler<StringEventArgs> OnNumberChanged;
		public event EventHandler<CallAnswerStateEventArgs> OnAnswerStateChanged;

		private string m_Name;
		private string m_Number;
		private eCallAnswerState m_AnswerState;

		#region Properties

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
		/// Gets the time that this incoming call started.
		/// </summary>
		public DateTime StartTime { get; private set; }

		/// <summary>
		/// Gets the time that this incoming call ended either by being answered, rejected or timeout.
		/// </summary>
		public DateTime? EndTime { get; private set; }

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

				if (m_AnswerState != eCallAnswerState.Unanswered && m_AnswerState != eCallAnswerState.Unknown)
					EndTime = IcdEnvironment.GetUtcTime();

				OnAnswerStateChanged.Raise(this, new CallAnswerStateEventArgs(m_AnswerState));
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

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		protected AbstractIncomingCall()
		{
			StartTime = IcdEnvironment.GetUtcTime();
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			OnNameChanged = null;
			OnNumberChanged = null;
			OnAnswerStateChanged = null;

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

		/// <summary>
		/// Gets the string representation for this instance.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			ReprBuilder builder = new ReprBuilder(this);

			builder.AppendProperty("Name", Name);

			return builder.ToString();
		}

		#endregion

		#region Private Methods

		private void Log(eSeverity severity, string message, params object[] args)
		{
			message = string.Format("{0} - {1}", this, message);
			ServiceProvider.GetService<ILoggerService>().AddEntry(severity, message, args);
		}

		#endregion

		#region Console

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

		#endregion
	}
}
