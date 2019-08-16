using System;
using System.Collections.Generic;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Directory.Tree;
using ICD.Connect.Conferencing.Contacts;

namespace ICD.Connect.Conferencing.Cisco.Comparers
{
	public sealed class ContactItemComparer : IComparer<IContact>
	{
		private static ContactItemComparer s_Instance;

		public static ContactItemComparer Instance
		{
			get { return s_Instance = s_Instance ?? new ContactItemComparer(); }
		}

		public int Compare(IContact x, IContact y)
		{
			CiscoContact ciscoContactX = x as CiscoContact;
			if (ciscoContactX == null)
				throw new ArgumentException("Expected a Cisco Contact");

			CiscoContact ciscoContactY = y as CiscoContact;
			if (ciscoContactY == null)
				throw new ArgumentException("Expected a Cisco Contact");

			return ciscoContactX.ItemNumber.CompareTo(ciscoContactY.ItemNumber);
		}
	}
}
