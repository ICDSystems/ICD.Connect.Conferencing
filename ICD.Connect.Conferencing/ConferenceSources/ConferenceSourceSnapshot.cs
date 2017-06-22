using System;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.ConferenceSources
{
	/// <summary>
	/// A ConferenceSourceSnapshot is a simple snapshot of the state of a source at a given time.
	/// </summary>
	public struct ConferenceSourceSnapshot
	{
		private readonly DateTime m_Time;
		private readonly string m_Name;
		private readonly string m_Number;
		private readonly eConferenceSourceType m_SourceType;
		private readonly eConferenceSourceStatus m_Status;
		private readonly eConferenceSourceDirection m_Direction;
		private readonly eConferenceSourceAnswerState m_AnswerState;

		#region Properties

		[PublicAPI]
		public DateTime Time { get { return m_Time; } }

		/// <summary>
		/// Gets the source name.
		/// </summary>
		[PublicAPI]
		public string Name { get { return m_Name; } }

		/// <summary>
		/// Gets the source number.
		/// </summary>
		[PublicAPI]
		public string Number { get { return m_Number; } }

		/// <summary>
		/// Gets the source type.
		/// </summary>
		[PublicAPI]
		public eConferenceSourceType SourceType { get { return m_SourceType; } }

		/// <summary>
		/// Call Status (Idle, Dialing, Ringing, etc)
		/// </summary>
		[PublicAPI]
		public eConferenceSourceStatus Status { get { return m_Status; } }

		/// <summary>
		/// Source direction (Incoming, Outgoing, etc)
		/// </summary>
		[PublicAPI]
		public eConferenceSourceDirection Direction { get { return m_Direction; } }

		/// <summary>
		/// Source Answer State (Ignored, Answered, etc)
		/// </summary>
		[PublicAPI]
		public eConferenceSourceAnswerState AnswerState { get { return m_AnswerState; } }

		#endregion

		#region Constructors

		/// <summary>
		/// Instantiates a ConferenceSourceSnapshot and sets the time to now.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="number"></param>
		/// <param name="sourceType"></param>
		/// <param name="status"></param>
		/// <param name="direction"></param>
		/// <param name="answerState"></param>
		public ConferenceSourceSnapshot(string name, string number, eConferenceSourceType sourceType,
		                                eConferenceSourceStatus status, eConferenceSourceDirection direction,
		                                eConferenceSourceAnswerState answerState)
			: this(IcdEnvironment.GetLocalTime(), name, number, sourceType, status, direction, answerState)
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="time"></param>
		/// <param name="name"></param>
		/// <param name="number"></param>
		/// <param name="sourceType"></param>
		/// <param name="status"></param>
		/// <param name="direction"></param>
		/// <param name="answerState"></param>
		public ConferenceSourceSnapshot(DateTime time, string name, string number, eConferenceSourceType sourceType,
		                                eConferenceSourceStatus status, eConferenceSourceDirection direction,
		                                eConferenceSourceAnswerState answerState)
		{
			m_Time = time;
			m_Name = name;
			m_Number = number;
			m_SourceType = sourceType;
			m_Status = status;
			m_Direction = direction;
			m_AnswerState = answerState;
		}

		/// <summary>
		/// Creates a snapshot from a given source.
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		public static ConferenceSourceSnapshot FromConferenceSource(IConferenceSource source)
		{
			return new ConferenceSourceSnapshot(IcdEnvironment.GetLocalTime(), source.Name, source.Number, source.SourceType,
			                                    source.Status,
			                                    source.Direction, source.AnswerState);
		}

		#endregion

		#region Methods

		/// <summary>
		/// Returns true if the snapshots are the same EXCEPT for the timestamp.
		/// </summary>
		/// <param name="current"></param>
		/// <returns></returns>
		public bool AreEqualExceptTimestamp(ConferenceSourceSnapshot current)
		{
			bool output = m_Name == current.Name;
			output &= m_Number == current.Number;
			output &= m_SourceType == current.SourceType;
			output &= m_Status == current.Status;
			output &= m_Direction == current.Direction;
			output &= m_AnswerState == current.AnswerState;

			return output;
		}

		/// <summary>
		/// Gets the string representation of the instance.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return
				string.Format("{0:MM/dd/yy H:mm:ss zzz} - Name: {1}, Number: {2}, Type: {3}, Status: {4}, Direction: {5}, Answer: {6}",
				              m_Time, m_Name, m_Number, m_SourceType, m_Status, m_Direction, m_AnswerState);
		}

		/// <summary>
		/// Implementing default equality.
		/// </summary>
		/// <param name="s1"></param>
		/// <param name="s2"></param>
		/// <returns></returns>
		public static bool operator ==(ConferenceSourceSnapshot s1, ConferenceSourceSnapshot s2)
		{
			return s1.Equals(s2);
		}

		/// <summary>
		/// Implementing default inequality.
		/// </summary>
		/// <param name="s1"></param>
		/// <param name="s2"></param>
		/// <returns></returns>
		public static bool operator !=(ConferenceSourceSnapshot s1, ConferenceSourceSnapshot s2)
		{
			return !(s1 == s2);
		}

		/// <summary>
		/// Returns true if this instance is equal to the given object.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override bool Equals(object other)
		{
			if (other == null || GetType() != other.GetType())
				return false;

			return GetHashCode() == ((ConferenceSourceSnapshot)other).GetHashCode();
		}

		/// <summary>
		/// Gets the hashcode for this instance.
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = hash * 23 + (int)m_Time.Ticks;
				hash = hash * 23 + (m_Name == null ? 0 : m_Name.GetHashCode());
				hash = hash * 23 + (m_Number == null ? 0 : m_Number.GetHashCode());
				hash = hash * 23 + (int)m_SourceType;
				hash = hash * 23 + (int)m_Status;
				hash = hash * 23 + (int)m_Direction;
				hash = hash * 23 + (int)m_AnswerState;
				return hash;
			}
		}

		#endregion
	}
}
