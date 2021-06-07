using System;
using System.Collections.Generic;
using ICD.Connect.Conferencing.Directory.Tree;

namespace ICD.Connect.Conferencing.Comparers
{
	public sealed class DirectoryFolderNameComparer : IComparer<IDirectoryFolder>
	{
		private static DirectoryFolderNameComparer s_Instance;

		public static DirectoryFolderNameComparer Instance { get { return s_Instance = s_Instance ?? new DirectoryFolderNameComparer(); } }

		public int Compare(IDirectoryFolder x, IDirectoryFolder y)
		{
			if (x == null && y == null)
				return 0;
			if (x == null)
				return -1;
			if (y == null)
				return 1;

			return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
		}
	}
}
