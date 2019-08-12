using System;
using System.Collections.Generic;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Directory.Tree;
using ICD.Connect.Conferencing.Directory.Tree;

namespace ICD.Connect.Conferencing.Cisco.Comparers
{
	public sealed class DirectoryFolderIdComparer : IComparer<IDirectoryFolder>
	{
		private static DirectoryFolderIdComparer s_Instance;

		public static DirectoryFolderIdComparer Instance
		{
			get { return s_Instance = s_Instance ?? new DirectoryFolderIdComparer(); }
		}

		public int Compare(IDirectoryFolder x, IDirectoryFolder y)
		{
			AbstractCiscoFolder ciscoFolderX = x as AbstractCiscoFolder;
			if (ciscoFolderX == null)
				throw new ArgumentException("Expected a Cisco Folder");

			AbstractCiscoFolder ciscoFolderY = y as AbstractCiscoFolder;
			if (ciscoFolderY == null)
				throw new ArgumentException("Expected a Cisco Folder");


			return string.Compare(ciscoFolderX.FolderId, ciscoFolderY.FolderId, StringComparison.Ordinal);
		}
	}
}
