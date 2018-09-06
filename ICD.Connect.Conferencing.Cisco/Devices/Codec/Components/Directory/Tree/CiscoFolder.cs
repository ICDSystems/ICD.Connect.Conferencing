using System.Collections.Generic;
using ICD.Common.Utils.Xml;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Directory.Tree
{
	public sealed class CiscoFolder : AbstractCiscoFolder
	{
		private readonly string m_Name;

		public override string Name { get { return m_Name; } }

		public CiscoFolder(string name, string folderId) : base(folderId)
		{
			m_Name = name;
		}

		/// <summary>
		/// Creates a Folder from a Folder XML Element.
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="idPrefix"></param>
		/// <param name="cache"></param>
		/// <returns></returns>
		public static CiscoFolder FromXml(string xml, string idPrefix, Dictionary<string, CiscoFolder> cache)
		{
			// From CE 9 onwards, LocalId is a child element, but from 8 back, it was an attribute.
			// So, try to get the child element first, and if not, load the attribute value instead.
			string folderId = XmlUtils.TryReadChildElementContentAsString(xml, "LocalId") ??
			                  XmlUtils.GetAttribute(xml, "localId").Value;
			string cachedId = idPrefix + folderId;

			if (!cache.ContainsKey(cachedId))
			{
				string name = XmlUtils.TryReadChildElementContentAsString(xml, "Name");
				cache[cachedId] = new CiscoFolder(name, folderId);
			}

			return cache[cachedId];
		}
	}
}