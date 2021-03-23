using ICD.Connect.Conferencing.Comparers;
using ICD.Connect.Conferencing.Directory.Tree;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.Directory
{
	public sealed class ZoomFolder : AbstractDirectoryFolder
	{
		private readonly string m_Name;

		public ZoomFolder(string name)
			: base(DirectoryFolderNameComparer.Instance, ContactNameComparer.Instance)
		{
			m_Name = name;
		}

		public override string Name
		{
			get { return m_Name; }
		}
	}
}