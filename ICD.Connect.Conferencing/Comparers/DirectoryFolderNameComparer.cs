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
			if (x == null)
				throw new ArgumentNullException("x");

			if (y == null)
				throw new ArgumentNullException("y");

			return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
		}
	}
}
