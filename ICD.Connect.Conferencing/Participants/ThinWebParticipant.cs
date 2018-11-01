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

	public delegate void ThinWebParticipantKickCallback(ThinWebParticipant sender);

	public delegate void ThinWebParticipantMuteCallback(ThinWebParticipant sender, bool mute);

	public sealed class ThinWebParticipant : IWebParticipant, IDisposable
	{
		public event EventHandler<ParticipantStatusEventArgs> OnStatusChanged;
		public event EventHandler<StringEventArgs> OnNameChanged;
		public event EventHandler<ParticipantTypeEventArgs> OnSourceTypeChanged;

		public ThinWebParticipantKickCallback KickCallback { get; set; }

		public ThinWebParticipantMuteCallback MuteCallback { get; set; }

		private string m_Name;
		private eParticipantStatus m_Status;
		private DateTime? m_Start;
		private DateTime? m_End;
		private eCallType m_SourceType;

		#region Properties

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
		/// Call Status (Idle, Dialing, Ringing, etc)
		/// </summary>
		public eParticipantStatus Status
		{
			get { return m_Status; }
			set
			{
				if (value == m_Status)
					return;

				m_Status = value;

				Log(eSeverity.Informational, "Status set to {0}", m_Status);

				OnStatusChanged.Raise(this, new ParticipantStatusEventArgs(m_Status));
			}
		}

		/// <summary>
		/// The time the conference ended.
		/// </summary>
		public DateTime? Start
		{
			get { return m_Start; }
			set
			{
				if (value == m_Start)
					return;

				m_Start = value;

				Log(eSeverity.Informational, "Start set to {0}", m_Start);
			}
		}

		/// <summary>
		/// The time the call ended.
		/// </summary>
		public DateTime? End
		{
			get { return m_End; }
			set
			{
				if (value == m_End)
					return;

				m_End = value;

				Log(eSeverity.Informational, "End set to {0}", m_End);
			}
		}

		/// <summary>
		/// Gets the source type.
		/// </summary>
		public eCallType SourceType
		{
			get { return m_SourceType; }
			set
			{
				if (value == m_SourceType)
					return;

				m_SourceType = value;

				Log(eSeverity.Informational, "SourceType set to {0}", m_SourceType);
			}
		}
		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public ThinWebParticipant()
		{
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			OnStatusChanged = null;
			OnNameChanged = null;
			OnSourceTypeChanged = null;

			KickCallback = null;
			MuteCallback = null;
		}

		#region Methods

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

		/// <summary>
		/// Disconnects the source.
		/// </summary>
		public void Kick()
		{
			ThinWebParticipantKickCallback handler = KickCallback;
			if (handler != null)
				handler(this);
		}

		/// <summary>
		/// Disconnects the source.
		/// </summary>
		public void Mute(bool mute)
		{
			ThinWebParticipantMuteCallback handler = MuteCallback;
			if (handler != null)
				handler(this, mute);
		}

		#endregion

		private void Log(eSeverity severity, string message, params object[] args)
		{
			message = string.Format("{0} - {1}", this, message);
			ServiceProvider.GetService<ILoggerService>().AddEntry(severity, message, args);
		}

		#region Console

		/// <summary>
		/// Gets the name of the node.
		/// </summary>
		public string ConsoleName { get { return "Participant"; } }

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public string ConsoleHelp { get { return string.Empty; } }

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
			addRow("Status", Status);
			addRow("Start", Start);
			addRow("End", End);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield return new ConsoleCommand("Kick", "Kicks the participant", () => Kick());
			yield return new GenericConsoleCommand<bool>("Mute", "Usage: Mute <true/false>", (m) => Mute(m));
		}

		#endregion
	}
}
