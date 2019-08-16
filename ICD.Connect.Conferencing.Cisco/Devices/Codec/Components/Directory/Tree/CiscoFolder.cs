using System;
using ICD.Common.Utils.Xml;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Directory.Tree
{
	[XmlConverter(typeof(CiscoFolderXmlConverter))]
	public sealed class CiscoFolder : AbstractCiscoFolder
	{
		private string m_Name;

		/// <summary>
		/// The name of the folder.
		/// </summary>
		public override string Name { get { return m_Name; } }

		/// <summary>
		/// Sets the name for the folder.
		/// </summary>
		/// <param name="name"></param>
		public void SetName(string name)
		{
			m_Name = name;
		}
	}

	public sealed class CiscoFolderXmlConverter : AbstractGenericXmlConverter<CiscoFolder>
	{
		// <Folder item="1" localId="localGroupId-3">
		//   <Name item="1">CA</Name>
		//   <FolderId item="1">localGroupId-3</FolderId>
		//   <ParentFolderId item="1">localGroupId-2</ParentFolderId>
		// </Folder>

		/// <summary>
		/// Creates a new instance of T.
		/// </summary>
		/// <returns></returns>
		protected override CiscoFolder Instantiate()
		{
			return new CiscoFolder();
		}

		/// <summary>
		/// Override to handle the current attribute.
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="instance"></param>
		protected override void ReadAttribute(IcdXmlReader reader, CiscoFolder instance)
		{
			switch (reader.Name)
			{
				case "localId":
					instance.FolderId = reader.Value;
					break;

				case "item":
					instance.ItemNumber = Int32.Parse(reader.Value);
					break;

				default:
					base.ReadAttribute(reader, instance);
					break;
			}
		}

		/// <summary>
		/// Override to handle the current element.
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="instance"></param>
		protected override void ReadElement(IcdXmlReader reader, CiscoFolder instance)
		{
			switch (reader.Name)
			{
				case "Name":
					string name = reader.ReadElementContentAsString();
					instance.SetName(name);
					break;
				case "LocalId":
					instance.FolderId = reader.ReadElementContentAsString();
					break;

				default:
					base.ReadElement(reader, instance);
					break;
			}
		}
	}
}