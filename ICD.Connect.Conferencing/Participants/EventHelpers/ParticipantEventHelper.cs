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
	public sealed class ParticipantEventHelper
	{
		private readonly SafeCriticalSection m_ParticipantsSection;
		private readonly List<IParticipant> m_SubscribedParticipants;
		private readonly Action<IParticipant> m_Callback;

		private Action<IParticipant> Callback
		{
			get { return m_Callback; }
		}

		public ParticipantEventHelper(Action<IParticipant> callback)
		{
			m_ParticipantsSection = new SafeCriticalSection();
			m_SubscribedParticipants = new List<IParticipant>();
			m_Callback = callback;
		}

		public void Dispose()
		{
			Clear();
		}

		#region Methods

		public void Subscribe(IParticipant participant)
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

		public void Unsubscribe(IParticipant participant)
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

		private void SubscribeInternal(IParticipant participant)
		{
			participant.OnNameChanged += ParticipantOnNameChanged;
			participant.OnParticipantTypeChanged += ParticipantOnParticipantTypeChanged;
			participant.OnStatusChanged += ParticipantOnStatusChanged;
			participant.OnNumberChanged += ParticipantOnNumberChanged;
		}

		private void UnsubscribeInternal(IParticipant participant)
		{
			participant.OnNameChanged -= ParticipantOnNameChanged;
			participant.OnParticipantTypeChanged -= ParticipantOnParticipantTypeChanged;
			participant.OnStatusChanged -= ParticipantOnStatusChanged;
			participant.OnNumberChanged -= ParticipantOnNumberChanged;
		}

		private void ParticipantOnStatusChanged(object sender, ParticipantStatusEventArgs participantStatusEventArgs)
		{
			Callback(sender as IParticipant);
		}

		private void ParticipantOnParticipantTypeChanged(object sender, CallTypeEventArgs callTypeEventArgs)
		{
			Callback(sender as IParticipant);
		}

		private void ParticipantOnNameChanged(object sender, StringEventArgs e)
		{
			Callback(sender as IParticipant);
		}

		private void ParticipantOnNumberChanged(object sender, StringEventArgs stringEventArgs)
		{
			Callback(sender as IParticipant);
		}

		#endregion
	}
}