using ICD.Common.Properties;
using ICD.Common.Utils.Xml;

namespace ICD.Connect.Conferencing.Cisco.Components.Video
{
	/// <summary>
	/// Represents a video input source.
	/// </summary>
	public sealed class VideoSource
	{
		#region Properties

		/// <summary>
		/// The id for the connector.
		/// </summary>
		[PublicAPI]
		public int ConnectorId { get; set; }

		/// <summary>
		/// The id for the source.
		/// </summary>
		[PublicAPI]
		public int SourceId { get; set; }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="sourceId"></param>
		/// <param name="connectorId"></param>
		public VideoSource(int sourceId, int connectorId)
		{
			SourceId = sourceId;
			ConnectorId = connectorId;
		}

		/// <summary>
		/// Parses an XML Source element and builds a VideoSource.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		public static VideoSource FromXml(string xml)
		{
			VideoSource output = new VideoSource(0, 0);
			output.UpdateFromXml(xml);

			return output;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Updates the values to match the xml.
		/// </summary>
		/// <param name="xml"></param>
		public void UpdateFromXml(string xml)
		{
			using (IcdXmlReader reader = new IcdXmlReader(xml))
			{
				reader.SkipToNextElement();

				SourceId = reader.GetAttributeAsInt("item");

				foreach (IcdXmlReader child in reader.GetChildElements())
				{
					switch (child.Name)
					{
						case "ConnectorId":
							ConnectorId = int.Parse(child.ReadInnerXml());
							break;
					}

					child.Dispose();
				}
			}
		}

		#endregion
	}
}
