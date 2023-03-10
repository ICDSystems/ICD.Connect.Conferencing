using ICD.Connect.Conferencing.Comparers;

namespace ICD.Connect.Conferencing.Directory.Tree
{
	public sealed class DirectoryFolder : AbstractDirectoryFolder
	{
		private readonly string m_Name;

		/// <summary>
		/// The name of the folder.
		/// </summary>
		public override string Name { get { return m_Name; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="name"></param>
		public DirectoryFolder(string name)
			: base(DirectoryFolderNameComparer.Instance, ContactNameComparer.Instance)
		{
			m_Name = name;
		}
	}
}
