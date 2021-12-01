using ICD.Common.Utils.Xml;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Directory.Tree
{
	[XmlConverter(typeof(ResultInfoXmlConverter))]
	public sealed class ResultInfo
	{
		public int Offset { get; set; }
		public int Limit { get; set; }
		public int TotalRows { get; set; }
	}

	public sealed class ResultInfoXmlConverter : AbstractGenericXmlConverter<ResultInfo>
	{
		//  <ResultInfo item = "1" >
		//    <Offset item="1">0</Offset>
		//    <Limit item="1">50</Limit>
		//    <TotalRows item="1">21</TotalRows>
		//  </ResultInfo>

		/// <summary>
		/// Creates a new instance of T.
		/// </summary>
		/// <returns></returns>
		protected override ResultInfo Instantiate()
		{
			return new ResultInfo();
		}

		/// <summary>
		/// Override to handle the current element.
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="instance"></param>
		protected override void ReadElement(IcdXmlReader reader, ResultInfo instance)
		{
			switch (reader.Name)
			{
				case "Offset":
					instance.Offset = reader.ReadElementContentAsInt();
					break;

				case "Limit":
					instance.Limit = reader.ReadElementContentAsInt();
					break;

				case "TotalRows":
					instance.TotalRows = reader.ReadElementContentAsInt();
					break;

				default:
					base.ReadElement(reader, instance);
					break;
			}
		}
	}
}
