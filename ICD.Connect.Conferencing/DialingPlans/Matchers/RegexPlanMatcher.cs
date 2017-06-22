using System.Text.RegularExpressions;
using ICD.Connect.Conferencing.ConferenceSources;

namespace ICD.Connect.Conferencing.DialingPlans.Matchers
{
	/// <summary>
	/// RegexPlanMatcher uses regex to match numbers.
	/// </summary>
	public sealed class RegexPlanMatcher : AbstractPlanMatcher
	{
		private readonly Regex m_Regex;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="sourceType"></param>
		/// <param name="formatMode"></param>
		/// <param name="regexString"></param>
		/// <param name="format"></param>
		public RegexPlanMatcher(string name, eConferenceSourceType sourceType, string format, eFormatMode formatMode,
		                        string regexString)
			: base(name, sourceType, format, formatMode)
		{
			m_Regex = new Regex(regexString, RegexOptions.Singleline | RegexOptions.Compiled);
		}

		/// <summary>
		/// Returns true if the number matches this plan.
		/// </summary>
		/// <param name="number"></param>
		/// <returns></returns>
		public override bool Matches(string number)
		{
			return m_Regex.IsMatch(number);
		}
	}
}
