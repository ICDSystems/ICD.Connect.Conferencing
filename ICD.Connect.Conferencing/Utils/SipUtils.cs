using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace ICD.Connect.Conferencing.Utils
{
	public static class SipUtils
	{
		private const string SIP_NUMBER_REGEX = @"^sip:([^@]*)@";

		// https://stackoverflow.com/questions/1547899/which-characters-make-a-url-invalid
		private static readonly char[] s_UriDisallowed =
		{
			(char)0x1F,
			(char)0x7F,
			(char)0x20,
			'<',
			'>',
			'#',
			'%',
			'"'
		};

		private static readonly char[] s_UriReserved =
		{
			';',
			'/',
			'?',
			':',
			'@',
			'&',
			'=',
			'+',
			'$',
			','
		};

		/// <summary>
		/// Returns the number portion from the given uri.
		/// </summary>
		/// <param name="uri"></param>
		/// <returns></returns>
		public static string NumberFromUri(string uri)
		{
			if (uri == null)
				throw new ArgumentNullException("uri");

			Regex regex = new Regex(SIP_NUMBER_REGEX);
			Match match = regex.Match(uri);

			return match.Success ? match.Groups[1].Value : null;
		}

		/// <summary>
		/// Returns true if the given number is a valid sip number.
		/// </summary>
		/// <param name="number"></param>
		/// <returns></returns>
		public static bool IsValidNumber(string number)
		{
			if (number == null)
				throw new ArgumentNullException("number");

			if (string.IsNullOrEmpty(number))
				return false;

			if (number.Any(char.IsWhiteSpace))
				return false;

			if (number.IndexOfAny(s_UriDisallowed) >= 0)
				return false;

			return number.IndexOfAny(s_UriReserved) < 0;
		}
	}
}
