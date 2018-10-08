using ICD.Connect.Conferencing.Directory.Tree;

namespace ICD.Connect.Conferencing.Zoom.Components.Directory
{
	public class ZoomFolder : AbstractDirectoryFolder
	{
		private readonly string m_Name;

		public ZoomFolder(string name)
		{
			m_Name = name;
		}

		public override string Name
		{
			get { return m_Name; }
		}
	}
}