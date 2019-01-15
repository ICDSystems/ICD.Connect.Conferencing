using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.Cameras;

namespace ICD.Connect.Conferencing.Participants
{
	public enum eCallDirection
	{
		Undefined = 0,
		Incoming = 1,
		Outgoing = 2
	}

	/// <summary>
	/// Answer state
	/// </summary>
	public enum eCallAnswerState
	{
		[UsedImplicitly] Unknown = 0,
		[UsedImplicitly] Unanswered = 1,
		Ignored = 2,
		Autoanswered = 3,
		Answered = 4
	}

	/// <summary>
	/// A traditional participant represents a conference participant using traditional
	/// conferencing protocols (SIP, H.323, PSTN/POTS)
	/// </summary>
	public interface ITraditionalParticipant : IParticipant
	{
		/// <summary>
		/// Raised when the source number changes.
		/// </summary>
		event EventHandler<StringEventArgs> OnNumberChanged;

		#region Properties

		/// <summary>
		/// Gets the number of the participant
		/// </summary>
		string Number { get; }

		/// <summary>
		/// Call Direction
		/// </summary>
		eCallDirection Direction { get; }

		/// <summary>
		/// Gets the time the call was dialed.
		/// </summary>
		DateTime DialTime { get; }

		/// <summary>
		/// Gets the remote camera.
		/// </summary>
		IRemoteCamera Camera { get; }

		#endregion

		#region Methods

		/// <summary>
		/// Holds the participant.
		/// </summary>
		void Hold();

		/// <summary>
		/// Resumes the participant.
		/// </summary>
		void Resume();

		/// <summary>
		/// Disconnects the participant.
		/// </summary>
		void Hangup();

		/// <summary>
		/// Sends DTMF to the participant.
		/// </summary>
		/// <param name="data"></param>
		void SendDtmf(string data);

		#endregion
	}

	public static class TraditionalParticipantExtensions
	{
		/// <summary>
		/// Allows sending data to dial-tone menus.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="data"></param>
		public static void SendDtmf(this ITraditionalParticipant extends, char data)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			extends.SendDtmf(data.ToString());
		}

		/// <summary>
		/// Gets the start time, falls through to dial time if no start time specified.
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static DateTime GetStartOrDialTime(this ITraditionalParticipant extends)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			return extends.Start ?? extends.DialTime;
		}
	}
}
