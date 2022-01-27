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
using ICD.Connect.Conferencing.Participants.Enums;

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

		/// <summary>
		/// Raised when the source number changes.
		/// </summary>
		public event EventHandler<StringEventArgs> OnNumberChanged;

		/// <summary>
		/// Raised when the participant is answered, dismissed or ignored.
		/// </summary>
		public event EventHandler<CallAnswerStateEventArgs> OnAnswerStateChanged;

		/// <summary>
		/// Raised when the participant's mute state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnIsMutedChanged;

		/// <summary>
		/// Raised when the participant's is self changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnIsSelfChanged;

		/// <summary>
		/// Raised when the participant's host state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnIsHostChanged;

		/// <summary>
		/// Raised when the participant's virtual hand raised state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnHandRaisedChanged;

		/// <summary>
		/// Raised when the supported participant features changes.
		/// </summary>
		public event EventHandler<ConferenceParticipantSupportedFeaturesChangedApiEventArgs> OnSupportedParticipantFeaturesChanged;

		#endregion

		#region Fields

		private string m_Name;
		private eCallType m_SourceType;
		private eParticipantStatus m_Status;
		private DateTime? m_Start;
		private DateTime? m_End;
		private string m_Number;
		private DateTime m_DialTime;
		private eCallDirection m_Direction;
		private eCallAnswerState m_AnswerState;
		private bool m_IsMuted;
		private bool m_IsHost;
		private bool m_IsSelf;
		private bool m_HandRaised;
		private bool m_CanRecord;
		private bool m_IsRecording;
		private eParticipantFeatures m_SupportedParticipantFeatures;

		#endregion

		#region Properties

		/// <summary>
		/// Gets the source name.
		/// </summary>
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

		/// <summary>
		/// Gets the participant's source type.
		/// </summary>
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

		/// <summary>
		/// Gets the participant's status (Idle, Dialing, Ringing, etc)
		/// </summary>
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

		/// <summary>
		/// The time when participant connected.
		/// </summary>
		public DateTime? StartTime
		{
			get { return m_Start; }
			protected set
			{
				if (m_Start == value)
					return;

				m_Start = value;

				Log(eSeverity.Informational, "StartTime set to {0}", m_Start);

				OnStartTimeChanged.Raise(this, new DateTimeNullableEventArgs(value));
			}
		}

		/// <summary>
		/// The time when participant disconnected.
		/// </summary>
		public DateTime? EndTime
		{
			get { return m_End; }
			protected set
			{
				if (m_End == value)
					return;

				m_End = value;

				Log(eSeverity.Informational, "EndTime set to {0}", m_End);

				OnEndTimeChanged.Raise(this, new DateTimeNullableEventArgs(value));
			}
		}

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

		/// <summary>
		/// Gets the time the call was dialed.
		/// </summary>
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

		/// <summary>
		/// Returns the answer state for the participant.
		/// Note, in order for a participant to exist, the call must be answered, so this value will be either answered or auto-answered always.
		/// </summary>
		public eCallAnswerState AnswerState
		{
			get { return m_AnswerState; }
			protected set
			{
				if (value == m_AnswerState)
					return;

				m_AnswerState = value;

				Log(eSeverity.Informational, "AnswerState set to {0}", m_AnswerState);

				OnAnswerStateChanged.Raise(this, new CallAnswerStateEventArgs(value));
			}
		}

		/// <summary>
		/// Whether or not the participant is muted.
		/// </summary>
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

		/// <summary>
		/// Whether or not the participant is the meeting host.
		/// </summary>
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

		/// <summary>
		/// The features supported by the participant.
		/// </summary>
		public eParticipantFeatures SupportedParticipantFeatures
		{
			get { return m_SupportedParticipantFeatures; }
			protected set
			{
				if (value == m_SupportedParticipantFeatures)
					return;

				m_SupportedParticipantFeatures = value;

				OnSupportedParticipantFeaturesChanged.Raise(this, new ConferenceParticipantSupportedFeaturesChangedApiEventArgs(m_SupportedParticipantFeatures));
			}
		}

		/// <summary>
		/// Whether or not the participant is the room itself.
		/// </summary>
		public bool IsSelf
		{
			get { return m_IsSelf; }
			protected set
			{
				if (m_IsSelf == value)
					return;

				m_IsSelf = value;

				HandleIsSelfChanged(value);

				Log(eSeverity.Informational, "IsSelf set to {0}", m_IsSelf);
				OnIsSelfChanged.Raise(this, m_IsSelf);
			}
		}

		/// <summary>
		/// Whether or not the participant's virtual hand is raised.
		/// </summary>
		public bool HandRaised
		{
			get { return m_HandRaised; }
			protected set
			{
				if (m_HandRaised == value)
					return;

				m_HandRaised = value;

				Log(eSeverity.Informational, "HandRaised set to {0}", m_IsSelf);
				OnHandRaisedChanged.Raise(this, m_HandRaised);
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
			OnStartTimeChanged = null;
			OnEndTimeChanged = null;
			OnNumberChanged = null;
			OnAnswerStateChanged = null;
			OnIsMutedChanged = null;
			OnIsHostChanged = null;
			OnHandRaisedChanged = null;
			OnSupportedParticipantFeaturesChanged = null;

			DisposeFinal();
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected virtual void DisposeFinal()
		{
		}

		protected void Log(eSeverity severity, string message, params object[] args)
		{
			var logger = ServiceProvider.TryGetService<ILoggerService>();
			if (logger == null)
				return;

			message = string.Format("{0} - {1}", this, message);
			logger.AddEntry(severity, message, args);
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

		public abstract void Admit();
		public abstract void Kick();
		public abstract void Mute(bool mute);
		public abstract void SetHandPosition(bool raised);

		/// <summary>
		/// Called to handle IsSelf changing
		/// </summary>
		/// <param name="value"></param>
		protected virtual void HandleIsSelfChanged(bool value)
		{
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
			addRow("StartTime", StartTime);
			addRow("EndTime", EndTime);
			addRow("Direction", Direction);
			addRow("DialTime", DialTime);
			addRow("Is Muted", IsMuted);
			addRow("Is Self", IsSelf);
			addRow("Is Host", IsHost);
			addRow("HandRaised", HandRaised);
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
