using System;
using System.Collections.Generic;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Directory.Tree;
using ICD.Connect.Conferencing.Contacts;

namespace ICD.Connect.Conferencing.Cisco.Comparers
{
	public sealed class ContactIdComparer : IComparer<IContact>
	{
		private static ContactIdComparer s_Instance;

		public static ContactIdComparer Instance
		{
			get { return s_Instance = s_Instance ?? new ContactIdComparer(); }
		}

		public int Compare(IContact x, IContact y)
		{
			CiscoContact ciscoContactX = x as CiscoContact;
			if (ciscoContactX == null)
				throw new ArgumentException("Expected a Cisco Contact");

			CiscoContact ciscoContactY = y as CiscoContact;
			if (ciscoContactY == null)
				throw new ArgumentException("Expected a Cisco Contact");


			return string.Compare(ciscoContactX.ContactId, ciscoContactY.ContactId, StringComparison.Ordinal);
		}
	}
}
