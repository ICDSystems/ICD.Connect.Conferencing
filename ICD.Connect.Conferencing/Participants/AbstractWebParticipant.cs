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

namespace ICD.Connect.Conferencing.Participants
{
	public abstract class AbstractWebParticipant : IWebParticipant, IDisposable
	{
		#region Events

		/// <summary>
		/// Raised when the participant's status changes.
		/// </summary>
		public event EventHandler<ParticipantStatusEventArgs> OnStatusChanged;

		/// <summary>
		/// Raised when the participant's source type changes.
		/// </summary>
		public event EventHandler<ParticipantTypeEventArgs> OnParticipantTypeChanged;

		/// <summary>
		/// Raised when the participant's name changes.
		/// </summary>
		public event EventHandler<StringEventArgs> OnNameChanged;

		/// <summary>
		/// Raised when the participant's mute state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnIsMutedChanged;

		/// <summary>
		/// Raised when the participant's host state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnIsHostChanged;

		#endregion

		private string m_Name;
		private eCallType m_SourceType;
		private eParticipantStatus m_Status;
		private bool m_IsMuted;
		private bool m_IsHost;
		private bool m_IsSelf;
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
				OnParticipantTypeChanged.Raise(this, new ParticipantTypeEventArgs(m_SourceType));
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

		public bool IsMuted
		{
			get { return m_IsMuted; }
			protected set
			{
				if (m_IsMuted == value)
					return;

				m_IsMuted = value;
				Log(eSeverity.Informational, "IsMuted set to {0}", m_IsMuted);
				OnIsMutedChanged.Raise(this, new BoolEventArgs(m_IsMuted));
			}
		}

		public bool IsHost
		{
			get { return m_IsHost; }
			protected set
			{
				if (m_IsHost == value)
					return;

				m_IsHost = value;
				Log(eSeverity.Informational, "IsHost set to {0}", m_IsHost);
				OnIsHostChanged.Raise(this, new BoolEventArgs(m_IsHost));
			}
		}
		
		public bool IsSelf
		{ 
			get { return m_IsSelf; }
			protected set
			{
				if (m_IsSelf == value)
					return;

				m_IsSelf = value;
				
				Log(eSeverity.Informational, "IsSelf set to {0}", m_IsSelf);
			} 
		}

		public DateTime? Start
		{ 
			get { return m_Start; }
			protected set
			{
				if (m_Start == value)
					return;

				m_Start = value;
				
				Log(eSeverity.Informational, "Start set to {0}", m_Start);
			} 
		}

		public DateTime? End
		{ 
			get { return m_End; }
			protected set
			{
				if (m_End == value)
					return;

				m_End = value;
				
				Log(eSeverity.Informational, "End set to {0}", m_End);
			} 
		}

		#endregion

		#region Methods

		public abstract void Kick();

		public abstract void Mute(bool mute);

		public void Dispose()
		{
			OnStatusChanged = null;
			OnParticipantTypeChanged = null;
			OnNameChanged = null;
			OnIsMutedChanged = null;
			OnIsHostChanged = null;

			DisposeFinal();
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

		protected virtual void DisposeFinal() { }

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
			yield break;
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
			addRow("Start", Start);
			addRow("End", End);
			addRow("Is Muted", IsMuted);
			addRow("Is Self", IsSelf);
			addRow("Is Host", IsHost);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public virtual IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield return new ConsoleCommand("Kick", "Kicks the participant", () => Kick());
			yield return new GenericConsoleCommand<bool>("Mute", "Usage: Mute <true/false>", m => Mute(m));
		}

		#endregion
	}
}
