using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;

namespace ICD.Connect.Conferencing.Conferences
{
	public abstract class AbstractConferenceBase<T> : IConference<T> where T : class, IParticipant, IDisposable
	{
		#region Fields

		private eConferenceStatus m_Status;
		private DateTime? m_Start;
		private DateTime? m_End;
		private eConferenceRecordingStatus m_RecordingStatus;
		private eConferenceFeatures m_SupportedConferenceFeatures;
		private string m_Name;
		private eCallType m_CallType;

		#endregion

		#region Events

		public abstract event EventHandler<ParticipantEventArgs> OnParticipantAdded;
		public abstract event EventHandler<ParticipantEventArgs> OnParticipantRemoved;

		/// <summary>
		/// Raised when the conference status changes.
		/// </summary>
		public virtual event EventHandler<ConferenceStatusEventArgs> OnStatusChanged;

		public virtual event EventHandler<StringEventArgs> OnNameChanged;

		/// <summary>
		/// Raised when the start time changes
		/// </summary>
		public virtual event EventHandler<DateTimeNullableEventArgs> OnStartTimeChanged;

		/// <summary>
		/// Raised when the end time changes
		/// </summary>
		public virtual event EventHandler<DateTimeNullableEventArgs> OnEndTimeChanged;

		/// <summary>
		/// Raised when the conference's call type changes.
		/// </summary>
		public virtual event EventHandler<GenericEventArgs<eCallType>> OnCallTypeChanged;

		/// <summary>
		/// Raised when the conference's recording status changes.
		/// </summary>
		public virtual event EventHandler<ConferenceRecordingStatusEventArgs> OnConferenceRecordingStatusChanged;

		/// <summary>
		/// Raised when the supported conference features changes.
		/// </summary>
		public virtual event EventHandler<GenericEventArgs<eConferenceFeatures>> OnSupportedConferenceFeaturesChanged;

		#endregion

		#region Properties

		/// <summary>
		/// Current conference status.
		/// </summary>
		public eConferenceStatus Status
		{
			get { return m_Status; }
			protected set
			{
				if(m_Status == value)
					return;

				m_Status = value;

				OnStatusChanged.Raise(this, value);
			}
		}

		/// <summary>
		/// Name of the conference
		/// </summary>
		public string Name
		{
			get { return m_Name; }
			protected set
			{
				if (m_Name == value)
					return;

				m_Name = value;

				OnNameChanged.Raise(this, value);
			}
		}

		/// <summary>
		/// The time the conference started.
		/// </summary>
		public DateTime? StartTime
		{
			get { return m_Start; }
			protected set
			{
				if (m_Start == value)
					return;

				m_Start = value;

				OnStartTimeChanged.Raise(this, new DateTimeNullableEventArgs(value));
			}
		}

		/// <summary>
		/// The time the conference ended.
		/// </summary>
		public DateTime? EndTime
		{
			get { return m_End; }
			protected set
			{
				if (m_End == value)
					return;

				m_End = value;

				OnEndTimeChanged.Raise(this, new DateTimeNullableEventArgs(value));
			}
		}

		/// <summary>
		/// Gets the type of call.
		/// </summary>
		public eCallType CallType
		{
			get { return m_CallType; }
			protected set
			{
				if (m_CallType == value)
					return;

				m_CallType = value;

				OnCallTypeChanged.Raise(this, value);
			}
		}

		/// <summary>
		/// Gets the status of the conference recording.
		/// </summary>
		public eConferenceRecordingStatus RecordingStatus
		{
			get { return m_RecordingStatus; }
			protected set
			{
				if (m_RecordingStatus == value)
					return;

				m_RecordingStatus = value;

				OnConferenceRecordingStatusChanged.Raise(this, new ConferenceRecordingStatusEventArgs(m_RecordingStatus));
			}
		}

		/// <summary>
		/// Gets the supported conference features.
		/// </summary>
		public eConferenceFeatures SupportedConferenceFeatures
		{
			get { return m_SupportedConferenceFeatures; }
			protected set
			{
				if (m_SupportedConferenceFeatures == value)
					return;

				m_SupportedConferenceFeatures = value;

				OnSupportedConferenceFeaturesChanged.Raise(this, new GenericEventArgs<eConferenceFeatures>(m_SupportedConferenceFeatures));
			}
		}

		/// <summary>
		/// Gets the name of the node.
		/// </summary>
		public virtual string ConsoleName { get { return GetType().Name; } }

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public virtual string ConsoleHelp { get { return string.Empty; }  }

		#endregion

		/// <summary>
		/// Gets the participants in this conference.
		/// </summary>
		/// <returns></returns>
		public abstract IEnumerable<T> GetParticipants();

		/// <summary>
		/// Leaves the conference, keeping the conference in tact for other participants.
		/// </summary>
		public abstract void LeaveConference();

		/// <summary>
		/// Ends the conference for all participants.
		/// </summary>
		public abstract void EndConference();

		/// <summary>
		/// Holds the conference
		/// </summary>
		public abstract void Hold();

		/// <summary>
		/// Resumes the conference
		/// </summary>
		public abstract void Resume();

		/// <summary>
		/// Sends DTMF to the participant.
		/// </summary>
		/// <param name="data"></param>
		public abstract void SendDtmf(string data);

		/// <summary>
		/// Starts recording the conference.
		/// </summary>
		public abstract void StartRecordingConference();

		/// <summary>
		/// Stops recording the conference.
		/// </summary>
		public abstract void StopRecordingConference();

		/// <summary>
		/// Pauses the current recording of the conference.
		/// </summary>
		public abstract void PauseRecordingConference();

		/// <summary>
		/// Gets the sources in this conference.
		/// </summary>
		/// <returns></returns>
		IEnumerable<IParticipant> IConference.GetParticipants()
		{
			return GetParticipants() as IEnumerable<IParticipant>;
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			OnStatusChanged = null;
			OnNameChanged = null;
			OnStartTimeChanged = null;
			OnEndTimeChanged = null;
			OnCallTypeChanged = null;
			OnConferenceRecordingStatusChanged = null;
			OnSupportedConferenceFeaturesChanged = null;

			DisposeFinal();
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected virtual void DisposeFinal()
		{
		}

		protected void UpdateStartAndEndTime()
		{
			UpdateStartTime();
			UpdateEndTime();
		}

		protected void UpdateStartTime()
		{
			DateTime? start;
			GetParticipants().Select(s => s.StartTime)
			                 .Where(s => s != null)
			                 .Order()
			                 .TryFirst(out start);
			if (start != null)
				StartTime = start;
		}

		protected void UpdateEndTime()
		{
			DateTime? end;
			GetParticipants().Select(e => e.EndTime)
			                 .Where(e => e != null)
			                 .Order()
			                 .TryFirst(out end);
			if (end != null)
				EndTime = end;
		}

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public virtual IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			yield return ConsoleNodeGroup.IndexNodeMap("Participants", "The collection of participants in this conference", GetParticipants());
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public virtual void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			addRow("Status", Status);
			addRow("StartTime", StartTime);
			addRow("EndTime", EndTime);
			addRow("CallType", CallType);
			addRow("ParticipantCount", GetParticipants().Count());
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public virtual IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield return new ConsoleCommand("Leave", "Leaves the conference", () => LeaveConference());
			yield return new ConsoleCommand("End", "Ends the conference", () => EndConference());
			yield return new ConsoleCommand("MuteAll", "Mutes all participants", () => this.MuteAll());
			yield return new ConsoleCommand("KickAll", "Kicks all participants", () => this.KickAll());
			yield return new ConsoleCommand("Hold", "Holds the call", () => Hold());
			yield return new ConsoleCommand("Resume", "Resumes the call", () => Resume());
			yield return new GenericConsoleCommand<string>("SendDTMF", "SendDTMF x", s => SendDtmf(s));
		}
	}
}