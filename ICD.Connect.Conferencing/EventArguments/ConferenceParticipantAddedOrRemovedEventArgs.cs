using System;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.Participants;

namespace ICD.Connect.Conferencing.EventArguments
{
	public sealed class ConferenceParticipantAddedOrRemovedEventArgs : EventArgs
	{
		/// <summary>
		/// Conference the participant was added/removed from
		/// </summary>
		public IConference Conference { get; private set; }

		/// <summary>
		/// If the participant was added or not
		/// True = added, False = removed
		/// </summary>
		public bool Added { get; private set; } 

		/// <summary>
		/// Partitipant that was added/removed
		/// </summary>
		public IParticipant Participant { get; private set; }

		public ConferenceParticipantAddedOrRemovedEventArgs(IConference conference, bool added, IParticipant participant)
		{
			Conference = conference;
			Added = added;
			Participant = participant;
		}
	}
}
