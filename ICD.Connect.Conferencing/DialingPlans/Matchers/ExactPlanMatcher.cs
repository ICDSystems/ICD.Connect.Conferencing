using System;
using ICD.Connect.Conferencing.ConferenceSources;

namespace ICD.Connect.Conferencing.DialingPlans.Matchers
{
	public sealed class ExactPlanMatcher : AbstractPlanMatcher
	{
		private readonly string m_Number;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="sourceType"></param>
		/// <param name="format"></param>
		/// <param name="formatMode"></param>
		/// <param name="number"></param>
		public ExactPlanMatcher(string name, eConferenceSourceType sourceType, string format, eFormatMode formatMode,
		                        string number)
			: base(name, sourceType, format, formatMode)
		{
			m_Number = number;
		}

		/// <summary>
		/// Returns true if the number matches this plan.
		/// </summary>
		/// <param name="number"></param>
		/// <returns></returns>
		public override bool Matches(string number)
		{
			return string.Equals(m_Number, number, StringComparison.InvariantCultureIgnoreCase);
		}
	}
}
