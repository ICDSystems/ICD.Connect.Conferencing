using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Conferencing.Directory.Tree
{
	public sealed class DirectoryFolderEventArgs : GenericEventArgs<IDirectoryFolder>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="folder"></param>
		public DirectoryFolderEventArgs(IDirectoryFolder folder)
			: base(folder)
		{
		}
	}
}
