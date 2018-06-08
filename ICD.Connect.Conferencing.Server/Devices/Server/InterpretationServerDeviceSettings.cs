﻿using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Xml;
using ICD.Connect.Devices.Simpl;
using ICD.Connect.Protocol.Network.Tcp;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Conferencing.Server.Devices.Server
{
	[KrangSettings("InterpretationServer", typeof(InterpretationServerDevice))]
	public sealed class InterpretationServerDeviceSettings : AbstractSimplDeviceSettings
	{
		private const string WRAPPED_DEVICES_ELEMENT = "Devices";
		private const string WRAPPED_DEVICE_ELEMENT = "Device";
		private const string SERVER_PORT_ELEMENT = "ServerPort";
		private const string SERVER_CLIENTS_ELEMENT = "ServerMaxClients";

		private readonly IcdHashSet<int> m_DeviceIds;

		public ushort ServerPort { get; set; }
		public int ServerMaxClients { get; set; }

		/// <summary>
		/// Constructor.
		/// </summary>
		public InterpretationServerDeviceSettings()
		{
			m_DeviceIds = new IcdHashSet<int>();
		}

		public void SetDeviceIds(IEnumerable<int> deviceIds)
		{
			if (deviceIds == null)
				throw new ArgumentNullException("deviceIds");

			m_DeviceIds.Clear();
			m_DeviceIds.AddRange(deviceIds);
		}

		public IEnumerable<int> GetDeviceIds()
		{
			return m_DeviceIds.Order().ToArray(m_DeviceIds.Count);
		}

		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			IEnumerable<int> deviceIds = XmlUtils.ReadListFromXml(xml, WRAPPED_DEVICES_ELEMENT, WRAPPED_DEVICE_ELEMENT,
			                                                      s => ParseDeviceToInt(s))
			                                     .ExceptNulls();

			SetDeviceIds(deviceIds);

			ServerPort = XmlUtils.TryReadChildElementContentAsUShort(xml, SERVER_PORT_ELEMENT) ?? 0;
			ServerMaxClients = XmlUtils.TryReadChildElementContentAsInt(xml, SERVER_CLIENTS_ELEMENT) ??
			                   AsyncTcpServer.MAX_NUMBER_OF_CLIENTS_SUPPORTED;
		}

		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			XmlUtils.WriteListToXml(writer, GetDeviceIds(), WRAPPED_DEVICES_ELEMENT, WRAPPED_DEVICE_ELEMENT);

			writer.WriteElementString(SERVER_PORT_ELEMENT, IcdXmlConvert.ToString(ServerPort));
			writer.WriteElementString(SERVER_CLIENTS_ELEMENT, IcdXmlConvert.ToString(ServerMaxClients));
		}

		private static int? ParseDeviceToInt(string s)
		{
			int id;
			bool isInt = StringUtils.TryParse(s, out id);

			return isInt ? id : (int?)null;
		}
	}
}
