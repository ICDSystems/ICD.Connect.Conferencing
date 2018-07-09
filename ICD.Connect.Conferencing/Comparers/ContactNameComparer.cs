using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.Comparers;

namespace ICD.Connect.Conferencing.Comparers
{
	public sealed class ContactNameComparer : IComparer<string>
	{
		private static readonly SequenceComparer<string> s_Comparer;

		private static ContactNameComparer s_Instance;

		public static ContactNameComparer Instance { get { return s_Instance = s_Instance ?? new ContactNameComparer(); } }

		/// <summary>
		/// Static constructor.
		/// </summary>
		static ContactNameComparer()
		{
			s_Comparer = new SequenceComparer<string>(StringComparer.Ordinal);
		}

		public int Compare(string x, string y)
		{
			if (x == null)
				throw new ArgumentNullException("x");

			if (y == null)
				throw new ArgumentNullException("y");

			// Compare names in reverse order
			IEnumerable<string> xSplit = x.Split().Where(s => !string.IsNullOrEmpty(s)).Reverse();
			IEnumerable<string> ySplit = y.Split().Where(s => !string.IsNullOrEmpty(s)).Reverse();

			return s_Comparer.Compare(xSplit, ySplit);
		}
	}
}
