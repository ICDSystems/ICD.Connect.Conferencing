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

		bool IsMuted { get; }
	}
}