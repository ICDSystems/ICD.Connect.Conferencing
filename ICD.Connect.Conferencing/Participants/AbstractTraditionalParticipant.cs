using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Conferencing.Participants
{
	public abstract class AbstractTraditionalParticipant : AbstractParticipant, ITraditionalParticipant
	{
		public event EventHandler<StringEventArgs> OnNumberChanged;

		private string m_Number;
		private DateTime m_DialTime;
		private eCallDirection m_Direction;
		private eCallAnswerState m_AnswerState;

		#region Properties

		/// <summary>
		/// Gets the source number.
		/// </summary>
		public string Number
		{
			get { return m_Number; }
			protected set
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
			protected set
			{
				if (value == m_Direction)
					return;

				m_Direction = value;

				Log(eSeverity.Informational, "Direction set to {0}", m_Direction);
			}
		}


		public DateTime DialTime
		{
			get { return m_DialTime; }
			protected set
			{
				if (value == m_DialTime)
					return;

				m_DialTime = value;

				Log(eSeverity.Informational, "Initiated set to {0}", m_DialTime);
			}
		}

		public eCallAnswerState AnswerState
		{
			get { return m_AnswerState; }
			protected set
			{
				if (value == m_AnswerState)
					return;

				m_AnswerState = value;

				Log(eSeverity.Informational, "AnswerState set to {0}", m_AnswerState);
			}
		}

		#endregion

		public abstract void Hold();
		public abstract void Resume();
		public abstract void Hangup();
		public abstract void SendDtmf(string data);


		#region Console

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);
			
			addRow("Direction", Direction);
			addRow("DialTime", DialTime);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new ConsoleCommand("Hold", "Holds the call", () => Hold());
			yield return new ConsoleCommand("Resume", "Resumes the call", () => Resume());
			yield return new ConsoleCommand("Hangup", "Ends the call", () => Hangup());
			yield return new GenericConsoleCommand<string>("SendDTMF", "SendDTMF x", s => SendDtmf(s));
		}

		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
