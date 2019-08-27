using System;
using System.Collections.Generic;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Directory.Tree;
using ICD.Connect.Conferencing.Directory.Tree;

namespace ICD.Connect.Conferencing.Cisco.Comparers
{
	public sealed class DirectoryFolderItemComparer : IComparer<IDirectoryFolder>
	{
		private static DirectoryFolderItemComparer s_Instance;

		public static DirectoryFolderItemComparer Instance
		{
			get { return s_Instance = s_Instance ?? new DirectoryFolderItemComparer(); }
		}

		public int Compare(IDirectoryFolder x, IDirectoryFolder y)
		{
			AbstractCiscoFolder ciscoFolderX = x as AbstractCiscoFolder;
			if (ciscoFolderX == null)
				throw new ArgumentException("Expected a Cisco Folder");

			AbstractCiscoFolder ciscoFolderY = y as AbstractCiscoFolder;
			if (ciscoFolderY == null)
				throw new ArgumentException("Expected a Cisco Folder");


			return ciscoFolderX.ItemNumber.CompareTo(ciscoFolderY.ItemNumber);
		}
	}
}
