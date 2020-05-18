using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Cameras;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.Participants
{
	public abstract class AbstractParticipant : IParticipant
	{
		#region Events

		/// <summary>
		/// Raised when the participant's status changes.
		/// </summary>
		public event EventHandler<ParticipantStatusEventArgs> OnStatusChanged;

		/// <summary>
		/// Raised when the participant source type changes.
		/// </summary>
		public event EventHandler<CallTypeEventArgs> OnParticipantTypeChanged;

		/// <summary>
		/// Raised when the participant's name changes.
		/// </summary>
		public event EventHandler<StringEventArgs> OnNameChanged;

		/// <summary>
		/// Raised when the participant's start time changes
		/// </summary>
		public event EventHandler<DateTimeNullableEventArgs> OnStartTimeChanged;

		/// <summary>
		/// Raised when the participant's end time changes
		/// </summary>
		public event EventHandler<DateTimeNullableEventArgs> OnEndTimeChanged;

		#endregion

		private string m_Name;
		private eCallType m_SourceType;
		private eParticipantStatus m_Status;
		private DateTime? m_Start;
		private DateTime? m_End;

		#region Properties

		public string Name
		{
			get { return m_Name; }
			protected set
			{
				if (m_Name == value)
					return;

				m_Name = value;
				Log(eSeverity.Informational, "Name set to {0}", m_Name);
				OnNameChanged.Raise(this, new StringEventArgs(m_Name));
			}
		}

		public eCallType CallType
		{
			get { return m_SourceType; }
			protected set
			{
				if (m_SourceType == value)
					return;

				m_SourceType = value;
				Log(eSeverity.Informational, "CallType set to {0}", m_SourceType);
				OnParticipantTypeChanged.Raise(this, new CallTypeEventArgs(m_SourceType));
			}
		}

		public eParticipantStatus Status
		{
			get { return m_Status; }
			protected set
			{
				if (m_Status == value)
					return;

				m_Status = value;
				Log(eSeverity.Informational, "Status set to {0}", m_Status);
				OnStatusChanged.Raise(this, new ParticipantStatusEventArgs(m_Status));
			}
		}

		public DateTime? StartTime
		{
			get { return m_Start; }
			protected set
			{
				if (m_Start == value)
					return;

				m_Start = value;

				Log(eSeverity.Informational, "Start set to {0}", m_Start);

				OnStartTimeChanged.Raise(this, new DateTimeNullableEventArgs(value));
			}
		}

		public DateTime? EndTime
		{
			get { return m_End; }
			protected set
			{
				if (m_End == value)
					return;

				m_End = value;

				Log(eSeverity.Informational, "End set to {0}", m_End);

				OnEndTimeChanged.Raise(this, new DateTimeNullableEventArgs(value));
			}
		}

		public abstract IRemoteCamera Camera { get; }

		#endregion

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			OnStatusChanged = null;
			OnParticipantTypeChanged = null;
			OnNameChanged = null;

			DisposeFinal();
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected virtual void DisposeFinal()
		{
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

		protected void Log(eSeverity severity, string message, params object[] args)
		{
			var logger = ServiceProvider.TryGetService<ILoggerService>();
			if (logger == null)
				return;

			message = string.Format("{0} - {1}", this, message);
			logger.AddEntry(severity, message, args);
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the name of the node.
		/// </summary>
		public virtual string ConsoleName { get { return string.IsNullOrEmpty(Name) ? GetType().Name : Name; } }

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public virtual string ConsoleHelp { get { return string.Empty; } }

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public virtual IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			if (Camera != null)
				yield return Camera;
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public virtual void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			addRow("Name", Name);
			addRow("Status", Status);
			addRow("CallType", CallType);
			addRow("Start", StartTime);
			addRow("End", EndTime);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public virtual IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield break;
		}

		#endregion
	}
}
