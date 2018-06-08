using System;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Cameras;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.ConferenceSources
{
	public enum eConferenceSourceDirection
	{
		Undefined,
		Incoming,
		Outgoing
	}

	/// <summary>
	/// Answer state
	/// </summary>
	public enum eConferenceSourceAnswerState
	{
		[UsedImplicitly] Unknown,
		[UsedImplicitly] Unanswered,
		Ignored,
		Autoanswered,
		Answered
	}

	/// <summary>
	/// A conference source represents a conferencing end-point (e.g. a telephone)
	/// </summary>
	public interface IConferenceSource : IConsoleNode
	{
		/// <summary>
		/// Raised when the answer state changes.
		/// </summary>
		event EventHandler<ConferenceSourceAnswerStateEventArgs> OnAnswerStateChanged;

		/// <summary>
		/// Raised when the call status changes.
		/// </summary>
		event EventHandler<ConferenceSourceStatusEventArgs> OnStatusChanged;

		/// <summary>
		/// Raised when the source name changes.
		/// </summary>
		event EventHandler<StringEventArgs> OnNameChanged;

		/// <summary>
		/// Raised when the source number changes.
		/// </summary>
		event EventHandler<StringEventArgs> OnNumberChanged;

		/// <summary>
		/// Raised when the source type changes.
		/// </summary>
		event EventHandler<ConferenceSourceTypeEventArgs> OnSourceTypeChanged;

		#region Properties

		/// <summary>
		/// Gets the source name.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets the source number.
		/// </summary>
		string Number { get; }

		/// <summary>
		/// Gets the source type.
		/// </summary>
		eConferenceSourceType SourceType { get; }

		/// <summary>
		/// Call Status (Idle, Dialing, Ringing, etc)
		/// </summary>
		eConferenceSourceStatus Status { get; }

		/// <summary>
		/// Source direction (Incoming, Outgoing, etc)
		/// </summary>
		eConferenceSourceDirection Direction { get; }

		/// <summary>
		/// Source Answer State (Ignored, Answered, etc)
		/// </summary>
		eConferenceSourceAnswerState AnswerState { get; }

		/// <summary>
		/// The time the conference ended.
		/// </summary>
		DateTime? Start { get; }

		/// <summary>
		/// The time the call ended.
		/// </summary>
		DateTime? End { get; }

		DateTime DialTime { get; }

		DateTime StartOrDialTime { get; }

		/// <summary>
		/// Gets the remote camera.
		/// </summary>
		IRemoteCamera Camera { get; }

		#endregion

		#region Methods

		/// <summary>
		/// Answers the incoming source.
		/// </summary>
		void Answer();

		/// <summary>
		/// Rejects the incoming source.
		/// </summary>
		void Reject();

		/// <summary>
		/// Holds the source.
		/// </summary>
		void Hold();

		/// <summary>
		/// Resumes the source.
		/// </summary>
		void Resume();

		/// <summary>
		/// Disconnects the source.
		/// </summary>
		void Hangup();

		/// <summary>
		/// Sends DTMF to the source.
		/// </summary>
		/// <param name="data"></param>
		void SendDtmf(string data);

		#endregion
	}

	public static class ConferenceSourceExtensions
	{
		/// <summary>
		/// Allows sending data to dial-tone menus.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="data"></param>
		public static void SendDtmf(this IConferenceSource extends, char data)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			extends.SendDtmf(data.ToString());
		}

		/// <summary>
		/// Gets the duration of the call.
		/// </summary>
		/// <param name="extends"></param>
		public static TimeSpan GetDuration(this IConferenceSource extends)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			if (extends.Start == null)
				return new TimeSpan();

			DateTime end = (extends.End != null) ? (DateTime)extends.End : IcdEnvironment.GetLocalTime();

			return end - (DateTime)extends.Start;
		}

		/// <summary>
		/// Returns true if the source is actively online.
		/// </summary>
		/// <param name="extends"></param>
		public static bool GetIsOnline(this IConferenceSource extends)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			switch (extends.Status)
			{
				case eConferenceSourceStatus.Undefined:
				case eConferenceSourceStatus.Dialing:
				case eConferenceSourceStatus.Connecting:
				case eConferenceSourceStatus.Ringing:
				case eConferenceSourceStatus.Disconnecting:
				case eConferenceSourceStatus.Disconnected:
				case eConferenceSourceStatus.Idle:
					return false;

				case eConferenceSourceStatus.Connected:
				case eConferenceSourceStatus.OnHold:
				case eConferenceSourceStatus.EarlyMedia:
				case eConferenceSourceStatus.Preserved:
				case eConferenceSourceStatus.RemotePreserved:
					return true;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// Returns true if the source has been answered.
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static bool GetIsAnswered(this IConferenceSource extends)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			switch (extends.AnswerState)
			{
				case eConferenceSourceAnswerState.Unknown:
				case eConferenceSourceAnswerState.Unanswered:
				case eConferenceSourceAnswerState.Ignored:
					return false;

				case eConferenceSourceAnswerState.Autoanswered:
				case eConferenceSourceAnswerState.Answered:
					return true;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}
