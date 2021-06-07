using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.Comparers;
using ICD.Connect.Conferencing.Contacts;

namespace ICD.Connect.Conferencing.Comparers
{
	public sealed class ContactNameComparer : IComparer<IContact>
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

		public int Compare(IContact x, IContact y)
		{
			if (x == null && y == null)
				return 0;
			if (x == null)
				return -1;
			if (y == null)
				return 1;

			// Compare names in reverse order
			IEnumerable<string> xSplit = x.Name.Split().Where(s => !string.IsNullOrEmpty(s)).Reverse();
			IEnumerable<string> ySplit = y.Name.Split().Where(s => !string.IsNullOrEmpty(s)).Reverse();

			return s_Comparer.Compare(xSplit, ySplit);
		}
	}
}
