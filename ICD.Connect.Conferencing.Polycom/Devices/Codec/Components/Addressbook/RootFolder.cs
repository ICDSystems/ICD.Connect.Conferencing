using ICD.Common.Utils;
using ICD.Connect.Conferencing.Directory.Tree;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Addressbook
{
	public sealed class RootFolder : AbstractDirectoryFolder
	{
		private readonly eAddressbookType m_Type;

		/// <summary>
		/// The name of the folder.
		/// </summary>
		public override string Name { get { return StringUtils.NiceName(m_Type); } }

		/// <summary>
		/// Gets the addressbook type.
		/// </summary>
		public eAddressbookType Type { get { return m_Type; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="type"></param>
		public RootFolder(eAddressbookType type)
		{
			m_Type = type;
		}
	}
}
