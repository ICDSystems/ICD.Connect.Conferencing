using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.IncomingCalls;
using ICD.Connect.Conferencing.Participants.Enums;

namespace ICD.Connect.Conferencing.ConferenceManagers.History
{
	public sealed class HistoricalIncomingConference : IHistoricalConference
	{
		[CanBeNull]
		private IIncomingCall m_IncomingCall;
		
		private readonly HistoricalIncomingParticipant m_Participant;
		
		private eConferenceStatus m_Status;
		private string m_Name;
		private string m_Number;
		private eCallDirection m_Direction;
		private eCallAnswerState m_AnswerState;

		#region Events

		/// <summary>
		/// Raised when the conference status changes.
		/// </summary>
		public event EventHandler<ConferenceStatusEventArgs> OnStatusChanged;

		public event EventHandler<StringEventArgs> OnNameChanged;
		public event EventHandler<StringEventArgs> OnNumberChanged;
		public event EventHandler<GenericEventArgs<eCallDirection>> OnDirectionChanged;
		public event EventHandler<GenericEventArgs<eCallAnswerState>> OnAnswerStateChanged;

		#endregion

		#region Properties

		public string Name
		{
			get { return m_Name; }
			private set
			{
				if (m_Name == value)
					return;

				m_Name = value;

				OnNameChanged.Raise(this, value);
			}
		}

		/// <summary>
		/// Number of the conferenc for redial, etc
		/// </summary>
		public string Number
		{
			get { return m_Number; }
			private set
			{
				if (m_Number == value)
					return;

				m_Number = value;

				OnNumberChanged.Raise(this, value);
			}
		}

		/// <summary>
		/// Direction
		/// </summary>
		public eCallDirection Direction
		{
			get { return m_Direction; }
			private set
			{
				if (m_Direction == value)
					return;

				m_Direction = value;

				OnDirectionChanged.Raise(this, value);
			}
		}

		public eCallAnswerState AnswerState
		{
			get { return m_AnswerState; }
			private set
			{
				if (m_AnswerState == value)
					return;

				m_AnswerState = value;

				OnAnswerStateChanged.Raise(this, value);
			}
		}

		/// <summary>
		/// Call Type
		/// </summary>
		public eCallType CallType { get; private set; }

		public DateTime? StartTime { get; private set; }
		public DateTime? EndTime { get; private set; }

		/// <summary>
		/// Gets the status of the conference
		/// </summary>
		public eConferenceStatus Status
		{
			get { return m_Status; }
			private set
			{
				if (m_Status == value)
					return;

				m_Status = value;

				OnStatusChanged.Raise(this, value);
			}
		}

		#endregion

		#region Constructor

		public HistoricalIncomingConference(IIncomingCall incomingCall)
		{
			m_IncomingCall = incomingCall;
			m_Participant = new HistoricalIncomingParticipant(incomingCall);
			Subscribe(m_IncomingCall);
			UpdateStatus(m_IncomingCall);
		}

		#endregion

		#region Methods

		public IEnumerable<IHistoricalParticipant> GetParticipants()
		{
			yield return m_Participant;
		}

		/// <summary>
		/// Detach HistoricalConference from the underlying conference/incoming call
		/// This is called when the conference gets removed, to unsubscribe
		/// and remove references to the conference
		/// </summary>
		public void Detach()
		{
			Unsubscribe(m_IncomingCall);
			m_IncomingCall = null;
			m_Participant.Detach();
		}

		#endregion

		#region IncomingCall Callbacks

		private void UpdateStatus(IIncomingCall incomingCall)
		{
			if (incomingCall == null)
				return;

			Name = incomingCall.Name;
			Number = incomingCall.Number;
			Direction = eCallDirection.Incoming;
			AnswerState = incomingCall.AnswerState;
			StartTime = incomingCall.StartTime;
			EndTime = incomingCall.EndTime;

			// Todo: Make Less Janky
			TraditionalIncomingCall traditional = incomingCall as TraditionalIncomingCall;
			CallType = traditional != null ? traditional.CallType : eCallType.Unknown;
		}

		private void Subscribe(IIncomingCall incomingCall)
		{
			if (incomingCall == null)
				return;

			incomingCall.OnNameChanged += IncomingCallOnNameChanged;
			incomingCall.OnNumberChanged += IncomingCallOnNumberChanged;
			incomingCall.OnAnswerStateChanged += IncomingCallOnAnswerStateChanged;
		}

		private void Unsubscribe(IIncomingCall incomingCall)
		{
			if (incomingCall == null)
				return;

			incomingCall.OnNameChanged -= IncomingCallOnNameChanged;
			incomingCall.OnNumberChanged -= IncomingCallOnNumberChanged;
			incomingCall.OnAnswerStateChanged -= IncomingCallOnAnswerStateChanged;
		}

		private void IncomingCallOnNameChanged(object sender, StringEventArgs args)
		{
			Name = args.Data;
		}

		private void IncomingCallOnNumberChanged(object sender, StringEventArgs args)
		{
			Number = args.Data;
		}

		private void IncomingCallOnAnswerStateChanged(object sender, CallAnswerStateEventArgs args)
		{
			AnswerState = args.Data;
		}

		#endregion
	}
}
