using System;
using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Conferencing.Server
{
	[KrangSettings(FACTORY_NAME)]
	public sealed class InterpretationAdapterSettings : AbstractDeviceSettings, IInterpretationAdapterSettings
	{
		private const string FACTORY_NAME = "InterpretationAdapter";

		public override string FactoryName { get { return FACTORY_NAME; } }

		public override Type OriginatorType { get { return typeof(InterpretationAdapter); } }

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);
		}
	}
}
