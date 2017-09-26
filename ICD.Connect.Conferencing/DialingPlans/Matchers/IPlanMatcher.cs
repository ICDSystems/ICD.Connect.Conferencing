using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.DialingPlans.Matchers
{
	public enum eFormatMode
	{
		Normal = 0,
		Safe
	}

	public interface IPlanMatcher
	{
		/// <summary>
		/// The source type of the plan.
		/// </summary>
		eConferenceSourceType SourceType { get; }

		/// <summary>
		/// Returns true if the number matches this plan.
		/// </summary>
		/// <param name="number"></param>
		/// <returns></returns>
		bool Matches(string number);

		/// <summary>
		/// Formats the number to a human readable format.
		/// </summary>
		/// <param name="number"></param>
		/// <returns></returns>
		string FormatNumber(string number);
	}
}
