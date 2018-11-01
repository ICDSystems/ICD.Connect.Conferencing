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

		public static bool IsValidSipUri(string number)
		{
			Uri uri;
			if (!Uri.TryCreate(number, UriKind.RelativeOrAbsolute, out uri))
				return false;

			return uri.IsWellFormedOriginalString() &&
			       (!uri.IsAbsoluteUri || uri.Scheme.Equals("sip", StringComparison.OrdinalIgnoreCase));
		}
	}
}
