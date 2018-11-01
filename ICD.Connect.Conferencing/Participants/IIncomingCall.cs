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

		event EventHandler<StringEventArgs> OnNameChanged;

		event EventHandler<StringEventArgs> OnNumberChanged;

		/// <summary>
		/// Source Answer State (Ignored, Answered, etc)
		/// </summary>
		eCallAnswerState AnswerState { get; }

		/// <summary>
		/// Source direction (Incoming, Outgoing, etc)
		/// </summary>
		eCallDirection Direction { get; }

		/// <summary>
		/// Answers the incoming call.
		/// </summary>
		void Answer();

		/// <summary>
		/// Rejects the incoming call.
		/// </summary>
		void Reject();

		/// <summary>
		/// Gets the number of the incoming call
		/// </summary>
		string Number { get; }

		/// <summary>
		/// Optional name associated with the incoming call
		/// </summary>
		string Name { get; }
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
	}
}