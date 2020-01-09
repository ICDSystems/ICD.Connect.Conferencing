using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.Participants
{
	public interface IIncomingCall : IConsoleNode
	{
		/// <summary>
		/// Raised when the answer state changes.
		/// </summary>
		event EventHandler<IncomingCallAnswerStateEventArgs> OnAnswerStateChanged;

		/// <summary>
		/// Raised when the name changes.
		/// </summary>
		event EventHandler<StringEventArgs> OnNameChanged;

		/// <summary>
		/// Raised when the number changes.
		/// </summary>
		event EventHandler<StringEventArgs> OnNumberChanged;

		/// <summary>
		/// Source Answer State (Ignored, Answered, etc)
		/// </summary>
		eCallAnswerState AnswerState { get; set; }

		/// <summary>
		/// Source direction (Incoming, Outgoing, etc)
		/// </summary>
		eCallDirection Direction { get; set; }

		/// <summary>
		/// Gets the number of the incoming call
		/// </summary>
		string Number { get; set; }

		/// <summary>
		/// Optional name associated with the incoming call
		/// </summary>
		string Name { get; set; }

		/// <summary>
		/// Gets the time that this incoming call started.
		/// </summary>
		DateTime StartTime { get; }

		/// <summary>
		/// Gets the time that this incoming call ended either by being answered, rejected or timeout.
		/// </summary>
		DateTime? EndTime { get; }

		/// <summary>
		/// Answers the incoming call.
		/// </summary>
		void Answer();

		/// <summary>
		/// Rejects the incoming call.
		/// </summary>
		void Reject();
	}

	public static class IncomingCallExtensions
	{
		/// <summary>
		/// Returns true if the source has been answered.
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static bool GetIsAnswered(this IIncomingCall extends)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			switch (extends.AnswerState)
			{
				case eCallAnswerState.Unknown:
				case eCallAnswerState.Unanswered:
				case eCallAnswerState.Ignored:
					return false;

				case eCallAnswerState.Autoanswered:
				case eCallAnswerState.Answered:
					return true;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// Returns true if the source is incoming and actively ringing.
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		[PublicAPI]
		public static bool GetIsRingingIncomingCall(this IIncomingCall extends)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			switch (extends.Direction)
			{
				case eCallDirection.Undefined:
				case eCallDirection.Outgoing:
					return false;

				case eCallDirection.Incoming:
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}

			switch (extends.AnswerState)
			{
				case eCallAnswerState.Answered:
				case eCallAnswerState.Autoanswered:
				case eCallAnswerState.Ignored:
					return false;

				case eCallAnswerState.Unknown:
				case eCallAnswerState.Unanswered:
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}
			return true;
		}

		/// <summary>
		/// Gets the end time for the incoming call.
		/// If no end time, returns the start time.
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static DateTime GetEndOrStartTime(this IIncomingCall extends)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			return extends.EndTime ?? extends.StartTime;
		}
	}
}