using System;
using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Conferencing.Participants
{
	/// <summary>
	/// A web participant represents a conference participant using web-based
	/// protocols, like Zoom, Skype, etc.
	/// </summary>
	public interface IWebParticipant : IParticipant
	{
		event EventHandler<BoolEventArgs> OnIsMutedChanged;

		event EventHandler<BoolEventArgs> OnIsHostChanged;

		/// <summary>
		/// Kick the participant from the conference.
		/// </summary>
		/// <returns></returns>
		void Kick();

		/// <summary>
		/// Mute the participant in the conference.
		/// </summary>
		/// <returns></returns>
		void Mute(bool mute);

		/// <summary>
		/// Whether or not the participant is muted.
		/// </summary>
		bool IsMuted { get; }

		/// <summary>
		/// Whether or not the participant is the room itself.
		/// </summary>
		bool IsSelf { get; }

		/// <summary>
		/// Whether or not the participant is the meeting host.
		/// </summary>
		bool IsHost { get; }
	}
}