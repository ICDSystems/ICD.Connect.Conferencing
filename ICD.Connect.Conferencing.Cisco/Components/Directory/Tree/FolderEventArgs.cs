﻿using ICD.Common.EventArguments;

namespace ICD.Connect.Conferencing.Cisco.Components.Directory.Tree
{
	/// <summary>
	/// FolderEventArgs is used with events regarding a IFolder.
	/// </summary>
	public sealed class FolderEventArgs : GenericEventArgs<IFolder>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="folder"></param>
		public FolderEventArgs(IFolder folder) : base(folder)
		{
		}
	}
}
