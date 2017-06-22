﻿using ICD.Common.Utils.Xml;

namespace ICD.Connect.Conferencing.Cisco.Components.Video.Connectors
{
	public sealed class VideoOutputConnector : AbstractVideoConnector
	{
		public enum eMonitorRole
		{
			Auto,
			First,
			Second,
			Third,
			PresentationOnly,
			Recorder
		}

		public eMonitorRole MonitorRole { get; private set; }

		/// <summary>
		/// Updates to match the xml values.
		/// </summary>
		/// <param name="xml"></param>
		public override void UpdateFromXml(string xml)
		{
			base.UpdateFromXml(xml);

			MonitorRole = XmlUtils.TryReadChildElementContentAsEnum<eMonitorRole>(xml, "MonitorRole", true) ?? MonitorRole;
		}
	}
}