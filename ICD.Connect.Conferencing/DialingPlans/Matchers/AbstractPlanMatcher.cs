using System;
using ICD.Common.Utils;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.DialingPlans.Matchers
{
	public abstract class AbstractPlanMatcher : IPlanMatcher
	{
		#region Properties

		/// <summary>
		/// The human readable name of the plan.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// The source type of the plan.
		/// </summary>
		public eConferenceSourceType SourceType { get; private set; }

		/// <summary>
		/// The format pattern for FormatNumber.
		/// </summary>
		public string Format { get; private set; }

		/// <summary>
		/// The format mode for FormatNumber.
		/// </summary>
		public eFormatMode FormatMode { get; private set; }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="sourceType"></param>
		/// <param name="format"></param>
		/// <param name="formatMode"></param>
		protected AbstractPlanMatcher(string name, eConferenceSourceType sourceType, string format, eFormatMode formatMode)
		{
			Name = name;
			SourceType = sourceType;
			Format = string.IsNullOrEmpty(format) ? string.Empty : format;
			FormatMode = formatMode;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Returns true if the number matches this plan.
		/// </summary>
		/// <param name="number"></param>
		/// <returns></returns>
		public abstract bool Matches(string number);

		/// <summary>
		/// Gets the string representation of this object.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			ReprBuilder builder = new ReprBuilder(this);

			builder.AppendProperty("Name", Name);
			builder.AppendProperty("SourceType", SourceType);

			return builder.ToString();
		}

		/// <summary>
		/// Formats the number to a human readable format.
		/// </summary>
		/// <param name="number"></param>
		/// <returns></returns>
		public virtual string FormatNumber(string number)
		{
			switch (FormatMode)
			{
				case eFormatMode.Normal:
					return string.Format(GetStringFormat(), number);
				case eFormatMode.Safe:
					return StringUtils.SafeNumericFormat(Format, number);

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		#endregion

		/// <summary>
		/// Returns the format string as a string.Format format.
		/// </summary>
		/// <returns></returns>
		private string GetStringFormat()
		{
			if (string.IsNullOrEmpty(Format))
				return "{0}";

			return "{0:" + Format + "}";
		}
	}
}
