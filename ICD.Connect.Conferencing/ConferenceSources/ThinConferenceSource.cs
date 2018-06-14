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

namespace ICD.Connect.Conferencing.ConferenceSources
{
	public delegate void ThinConferenceSourceAnswerCallback(ThinConferenceSource sender);

	public delegate void ThinConferenceSourceRejectCallback(ThinConferenceSource sender);

	public delegate void ThinConferenceSourceHoldCallback(ThinConferenceSource sender);

	public delegate void ThinConferenceSourceResumeCallback(ThinConferenceSource sender);

	public delegate void ThinConferenceSourceSendDtmfCallback(ThinConferenceSource sender, string data);

	public delegate void ThinConferenceSourceHangupCallback(ThinConferenceSource sender);

	public sealed class ThinConferenceSource : IConferenceSource, IDisposable
	{
		public event EventHandler<ConferenceSourceAnswerStateEventArgs> OnAnswerStateChanged;
		public event EventHandler<ConferenceSourceStatusEventArgs> OnStatusChanged;
		public event EventHandler<StringEventArgs> OnNameChanged;
		public event EventHandler<StringEventArgs> OnNumberChanged;
		public event EventHandler<ConferenceSourceTypeEventArgs> OnSourceTypeChanged;

		public ThinConferenceSourceAnswerCallback AnswerCallback { get; set; }
		public ThinConferenceSourceRejectCallback RejectCallback { get; set; }
		public ThinConferenceSourceHoldCallback HoldCallback { get; set; }
		public ThinConferenceSourceResumeCallback ResumeCallback { get; set; }
		public ThinConferenceSourceSendDtmfCallback SendDtmfCallback { get; set; }
		public ThinConferenceSourceHangupCallback HangupCallback { get; set; }

		private string m_Name;
		private string m_Number;
		private eConferenceSourceStatus m_Status;
		private eConferenceSourceAnswerState m_AnswerState;
		private DateTime? m_Start;
		private DateTime? m_End;
		private DateTime m_DialTime;
		private eConferenceSourceDirection m_Direction;

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
		public eConferenceSourceStatus Status
		{
			get { return m_Status; }
			set
			{
				if (value == m_Status)
					return;

				m_Status = value;

				Log(eSeverity.Informational, "Status set to {0}", m_Status);

				OnStatusChanged.Raise(this, new ConferenceSourceStatusEventArgs(m_Status));
			}
		}

		/// <summary>
		/// Source direction (Incoming, Outgoing, etc)
		/// </summary>
		public eConferenceSourceDirection Direction
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
		public eConferenceSourceAnswerState AnswerState
		{
			get { return m_AnswerState; }
			set
			{
				if (value == m_AnswerState)
					return;

				m_AnswerState = value;

				Log(eSeverity.Informational, "AnswerState set to {0}", m_AnswerState);

				OnAnswerStateChanged.Raise(this, new ConferenceSourceAnswerStateEventArgs(m_AnswerState));
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

		public DateTime StartOrDialTime { get { return Start ?? DialTime; } }

		/// <summary>
		/// Gets the source type.
		/// </summary>
		eConferenceSourceType IConferenceSource.SourceType { get { return eConferenceSourceType.Audio; } }

		/// <summary>
		/// Gets the remote camera.
		/// </summary>
		IRemoteCamera IConferenceSource.Camera { get { return null; } }

		#endregion

		public ThinConferenceSource()
		{
			DialTime = IcdEnvironment.GetLocalTime();
		}

		public void Dispose()
		{
			AnswerCallback = null;
			RejectCallback = null;
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
		/// Answers the incoming source.
		/// </summary>
		public void Answer()
		{
			ThinConferenceSourceAnswerCallback handler = AnswerCallback;
			if (handler != null)
				handler(this);
		}

		/// <summary>
		/// Rejects the incoming source.
		/// </summary>
		public void Reject()
		{
			ThinConferenceSourceRejectCallback handler = RejectCallback;
			if (handler != null)
				handler(this);
		}

		/// <summary>
		/// Holds the source.
		/// </summary>
		public void Hold()
		{
			ThinConferenceSourceHoldCallback handler = HoldCallback;
			if (handler != null)
				handler(this);
		}

		/// <summary>
		/// Resumes the source.
		/// </summary>
		public void Resume()
		{
			ThinConferenceSourceResumeCallback handler = ResumeCallback;
			if (handler != null)
				handler(this);
		}

		/// <summary>
		/// Sends DTMF to the source.
		/// </summary>
		/// <param name="data"></param>
		public void SendDtmf(string data)
		{
			ThinConferenceSourceSendDtmfCallback handler = SendDtmfCallback;
			if (handler != null)
				handler(this, data);
		}

		/// <summary>
		/// Disconnects the source.
		/// </summary>
		public void Hangup()
		{
			ThinConferenceSourceHangupCallback handler = HangupCallback;
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
		public string ConsoleName { get { return "ConferenceSource"; } }

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
			addRow("AnswerState", AnswerState);
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
			yield return new ConsoleCommand("Answer", "Answers the incoming call", () => Answer());
			yield return new ConsoleCommand("Reject", "Rejects the incoming call", () => Reject());
			yield return new GenericConsoleCommand<string>("SendDTMF", "SendDTMF x", s => SendDtmf(s));
		}

		#endregion
	}
}
