using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Connect.Conferencing.Participants;

namespace ICD.Connect.Conferencing.Server.Conferences
{
	public sealed class ParticipantInfo
	{

		private readonly string m_Name;
		private readonly string m_Number;

		[CanBeNull]
		public string Name{ get { return m_Name; }}

		[CanBeNull]
		public string Number { get { return m_Number; } }

		public ParticipantInfo([CanBeNull] string name, [CanBeNull] string number)
		{
			m_Name = name;
			m_Number = number;
		}

		public ParticipantInfo([NotNull] ParticipantState state)
		{
			if (state == null)
				throw new ArgumentNullException("state");

			m_Name = state.Name;
			m_Number = state.Number;
		}

		public ParticipantInfo([NotNull] IParticipant participant)
		{
			if (participant == null)
				throw new ArgumentNullException("participant");

			m_Name = participant.Name;
			m_Number = participant.Number;
		}
	}

	public sealed class ParticipantInfoEqualityComparer : EqualityComparer<ParticipantInfo>
	{
		private static ParticipantInfoEqualityComparer s_Instance;

		public static ParticipantInfoEqualityComparer Instance
		{
			get { return s_Instance ?? (s_Instance = new ParticipantInfoEqualityComparer()); }
		}

		public override bool Equals(ParticipantInfo x, ParticipantInfo y)
		{
			if (x == null)
				return y == null;

			return ((x.Name == null && y.Name == null) || (x.Name != null &&
														   x.Name.Equals(y.Name, StringComparison.Ordinal))) &&
				   ((x.Number == null && y.Number == null) || (x.Number != null &&
															   x.Number.Equals(y.Number, StringComparison.Ordinal)));
			
		}

		public override int GetHashCode(ParticipantInfo obj)
		{
			if (obj == null)
				return 0;

			int hashCode = 17;

			if (obj.Name != null)
				hashCode = obj.Name.GetHashCode() + (hashCode * 17);
			if (obj.Number != null)
				hashCode = obj.Number.GetHashCode() + (hashCode * 17);

			return hashCode;
		}
	}
}