using System.Text.RegularExpressions;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.DialingPlans.Matchers
{
	/// <summary>
	/// RegexPlanMatcher uses regex to match numbers.
	/// </summary>
	public sealed class RegexPlanMatcher : AbstractPlanMatcher
	{
		private readonly string m_Pattern;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="sourceType"></param>
		/// <param name="formatMode"></param>
		/// <param name="pattern"></param>
		/// <param name="format"></param>
		public RegexPlanMatcher(string name, eCallType sourceType, string format, eFormatMode formatMode,
		                        string pattern)
			: base(name, sourceType, format, formatMode)
		{
			m_Pattern = pattern;
		}

		/// <summary>
		/// Returns true if the number matches this plan.
		/// </summary>
		/// <param name="number"></param>
		/// <returns></returns>
		public override bool Matches(string number)
		{
			return Regex.IsMatch(number, m_Pattern, RegexOptions.Singleline);
		}
	}
}
