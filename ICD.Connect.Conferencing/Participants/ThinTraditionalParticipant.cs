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

	public delegate void ThinParticipantHoldCallback(ThinTraditionalParticipant sender);

	public delegate void ThinParticipantResumeCallback(ThinTraditionalParticipant sender);

	public delegate void ThinParticipantSendDtmfCallback(ThinTraditionalParticipant sender, string data);

	public delegate void ThinParticipantHangupCallback(ThinTraditionalParticipant sender);

	public sealed class ThinTraditionalParticipant : ITraditionalParticipant, IDisposable
	{
		public event EventHandler<ParticipantStatusEventArgs> OnStatusChanged;
		public event EventHandler<StringEventArgs> OnNameChanged;
		public event EventHandler<StringEventArgs> OnNumberChanged;
		public event EventHandler<ParticipantTypeEventArgs> OnSourceTypeChanged;

		public ThinParticipantHoldCallback HoldCallback { get; set; }
		public ThinParticipantResumeCallback ResumeCallback { get; set; }
		public ThinParticipantSendDtmfCallback SendDtmfCallback { get; set; }
		public ThinParticipantHangupCallback HangupCallback { get; set; }

		private string m_Name;
		private string m_Number;
		private eParticipantStatus m_Status;
		private eCallAnswerState m_AnswerState;
		private DateTime? m_Start;
		private DateTime? m_End;
		private DateTime m_DialTime;
		private eCallDirection m_Direction;
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

		public DateTime DialTime
		{
			get { return m_DialTime; }
			set
			{
				if (value == m_DialTime)
					return;

				m_DialTime = value;

				Log(eSeverity.Informational, "Initiated set to {0}", m_DialTime);
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

		/// <summary>
		/// Gets the remote camera.
		/// </summary>
		IRemoteCamera ITraditionalParticipant.Camera { get { return null; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public ThinTraditionalParticipant()
		{
			DialTime = IcdEnvironment.GetLocalTime();
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			OnStatusChanged = null;
			OnNameChanged = null;
			OnNumberChanged = null;
			OnSourceTypeChanged = null;

			HoldCallback = null;
			ResumeCallback = null;
			SendDtmfCallback = null;
			HangupCallback = null;
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
			builder.AppendProperty("Number", Number);

			return builder.ToString();
		}

		/// <summary>
		/// Holds the source.
		/// </summary>
		public void Hold()
		{
			ThinParticipantHoldCallback handler = HoldCallback;
			if (handler != null)
				handler(this);
		}

		/// <summary>
		/// Resumes the source.
		/// </summary>
		public void Resume()
		{
			ThinParticipantResumeCallback handler = ResumeCallback;
			if (handler != null)
				handler(this);
		}

		/// <summary>
		/// Sends DTMF to the source.
		/// </summary>
		/// <param name="data"></param>
		public void SendDtmf(string data)
		{
			ThinParticipantSendDtmfCallback handler = SendDtmfCallback;
			if (handler != null)
				handler(this, data ?? string.Empty);
		}

		/// <summary>
		/// Disconnects the source.
		/// </summary>
		public void Hangup()
		{
			ThinParticipantHangupCallback handler = HangupCallback;
			if (handler != null)
				handler(this);
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
			addRow("Number", Number);
			addRow("Status", Status);
			addRow("Direction", Direction);
			addRow("Start", Start);
			addRow("End", End);
			addRow("DialTime", DialTime);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield return new ConsoleCommand("Hold", "Holds the call", () => Hold());
			yield return new ConsoleCommand("Resume", "Resumes the call", () => Resume());
			yield return new ConsoleCommand("Hangup", "Ends the call", () => Hangup());
			yield return new GenericConsoleCommand<string>("SendDTMF", "SendDTMF x", s => SendDtmf(s));
		}

		#endregion
	}
}
