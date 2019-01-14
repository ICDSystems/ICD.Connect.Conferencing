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

		/*
		/// <summary>
		/// Returns true if the source is incoming and actively ringing.
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		// TODO: REMOVE OLD CODE
		//[PublicAPI]
		//public static bool GetIsRingingIncomingCall(this ITraditionalParticipant extends)
		//{
		//    if (extends == null)
		//        throw new ArgumentNullException("extends");

		//    switch (extends.Direction)
		//    {
		//        case eCallDirection.Undefined:
		//        case eCallDirection.Outgoing:
		//            return false;

		//        case eCallDirection.Incoming:
		//            break;

		//        default:
		//            throw new ArgumentOutOfRangeException();
		//    }

		//    switch (extends.AnswerState)
		//    {
		//        case eCallAnswerState.Answered:
		//        case eCallAnswerState.Autoanswered:
		//        case eCallAnswerState.Ignored:
		//            return false;

		//        case eCallAnswerState.Unknown:
		//        case eCallAnswerState.Unanswered:
		//            break;

		//        default:
		//            throw new ArgumentOutOfRangeException();
		//    }

		//    switch (extends.Status)
		//    {
		//        case eParticipantStatus.Undefined:
		//        case eParticipantStatus.Connected:
		//        case eParticipantStatus.Disconnecting:
		//        case eParticipantStatus.OnHold:
		//        case eParticipantStatus.EarlyMedia:
		//        case eParticipantStatus.Preserved:
		//        case eParticipantStatus.RemotePreserved:
		//        case eParticipantStatus.Disconnected:
		//        case eParticipantStatus.Idle:
		//            return false;

		//        case eParticipantStatus.Dialing:
		//        case eParticipantStatus.Ringing:
		//        case eParticipantStatus.Connecting:
		//            return true;

		//        default:
		//            throw new ArgumentOutOfRangeException();
		//    }
		//}
		*/

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
