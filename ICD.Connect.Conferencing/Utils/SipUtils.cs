using System;
using System.Text.RegularExpressions;
using ICD.Common.Utils;

namespace ICD.Connect.Conferencing.Utils
{
	public static class SipUtils
	{
		private const string SIP_NUMBER_REGEX = @"^sip:([^@]*)@";

		/// <summary>
		/// Returns the number portion from the given uri.
		/// </summary>
		/// <param name="uri"></param>
		/// <returns></returns>
		public static string NumberFromUri(string uri)
		{
			if (uri == null)
				throw new ArgumentNullException("uri");

			Match match;
			return RegexUtils.Matches(uri, SIP_NUMBER_REGEX, out match) ? match.Groups[1].Value : null;
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
