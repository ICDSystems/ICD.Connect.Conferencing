using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.ConferenceSources;

namespace ICD.Connect.Conferencing.DialingPlans.Matchers
{
	/// <summary>
	/// NumericPlanMatcher checks to see if the number is numeric.
	/// </summary>
	public sealed class NumericPlanMatcher : AbstractPlanMatcher
	{
		/// <summary>
		/// The min number of digits for the number.
		/// </summary>
		private readonly int m_MinLength;

		/// <summary>
		/// The max number of digits for the number.
		/// </summary>
		private readonly int m_MaxLength;

		/// <summary>
		/// Characters to ignore from the number (e.g. ignore '-' '.' in phone numbers)
		/// </summary>
		private readonly char[] m_Exclude;

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="sourceType"></param>
		/// <param name="formatMode"></param>
		/// <param name="minLength"></param>
		/// <param name="maxLength"></param>
		/// <param name="exclude"></param>
		/// <param name="format"></param>
		public NumericPlanMatcher(string name, eConferenceSourceType sourceType, string format, eFormatMode formatMode,
		                          int minLength, int maxLength, char[] exclude)
			: base(name, sourceType, format, formatMode)
		{
			m_MinLength = minLength;
			m_MaxLength = maxLength;
			m_Exclude = exclude;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Returns true if the number matches this plan.
		/// </summary>
		/// <param name="number"></param>
		/// <returns></returns>
		public override bool Matches(string number)
		{
			number = GetExcluded(number);

			if (number.Length < m_MinLength)
				return false;

			if (m_MaxLength > 0 && number.Length > m_MaxLength)
				return false;

			return number.IsNumeric();
		}

		/// <summary>
		/// Formats the number to a human readable format.
		/// </summary>
		/// <param name="number"></param>
		/// <returns></returns>
		public override string FormatNumber(string number)
		{
			number = GetExcluded(number);
			return base.FormatNumber(number);
		}

		#endregion

		/// <summary>
		/// Removes whitespace and exclude characters from the string.
		/// </summary>
		/// <param name="number"></param>
		/// <returns></returns>
		private string GetExcluded(string number)
		{
			number = number.RemoveWhitespace();
			return number.Remove(m_Exclude);
		}
	}
}
