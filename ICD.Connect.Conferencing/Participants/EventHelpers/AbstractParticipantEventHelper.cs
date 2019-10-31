using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.Participants.EventHelpers
{
	/// <summary>
	/// Used to easily subscribe to all IParticipant events with one common callback
	/// </summary>
	public abstract class AbstractParticipantEventHelper<T>: IDisposable where T: class, IParticipant
	{
		private readonly SafeCriticalSection m_ParticipantsSection;
		private readonly List<T> m_SubscribedParticipants;
		private readonly Action<T> m_Callback;

		protected Action<T> Callback
		{
			get { return m_Callback; }
		}

		protected AbstractParticipantEventHelper(Action<T> callback)
		{
			m_ParticipantsSection = new SafeCriticalSection();
			m_SubscribedParticipants = new List<T>();
			m_Callback = callback;
		}

		public void Dispose()
		{
			Clear();
		}

		#region Methods

		public void Subscribe(T participant)
		{
			if (participant == null)
				return;

			m_ParticipantsSection.Enter();
			try
			{
				if (m_SubscribedParticipants.Contains(participant))
					return;

				SubscribeInternal(participant);
				m_SubscribedParticipants.Add(participant);
			}
			finally
			{
				m_ParticipantsSection.Leave();
			}
		}

		public void Unsubscribe(T participant)
		{
			if (participant == null)
				return;

			m_ParticipantsSection.Enter();
			try
			{
				if (!m_SubscribedParticipants.Contains(participant))
					return;

				UnsubscribeInternal(participant);
				m_SubscribedParticipants.Remove(participant);
			}
			finally
			{
				m_ParticipantsSection.Leave();
			}
		}

		public void Clear()
		{
			m_ParticipantsSection.Enter();
			try
			{
				foreach (var participant in m_SubscribedParticipants.ToList())
					Unsubscribe(participant);
			}
			finally
			{
				m_ParticipantsSection.Leave();
			}
		}

		#endregion

		#region IParticipant Callbacks

		protected virtual void SubscribeInternal(T participant)
		{
			participant.OnNameChanged += ParticipantOnNameChanged;
			participant.OnParticipantTypeChanged += ParticipantOnParticipantTypeChanged;
			participant.OnStatusChanged += ParticipantOnStatusChanged;
		}

		protected virtual void UnsubscribeInternal(T participant)
		{
			participant.OnNameChanged -= ParticipantOnNameChanged;
			participant.OnParticipantTypeChanged -= ParticipantOnParticipantTypeChanged;
			participant.OnStatusChanged -= ParticipantOnStatusChanged;
		}

		private void ParticipantOnStatusChanged(object sender, ParticipantStatusEventArgs participantStatusEventArgs)
		{
			Callback(sender as T);
		}

		private void ParticipantOnParticipantTypeChanged(object sender, ParticipantTypeEventArgs participantTypeEventArgs)
		{
			Callback(sender as T);
		}

		private void ParticipantOnNameChanged(object sender, StringEventArgs e)
		{
			Callback(sender as T);
		}

		#endregion
	}
}