using ICD.Common.Utils.Xml;

namespace ICD.Connect.Conferencing.Cisco.Components.Video.Connectors
{
	public sealed class VideoInputConnector : AbstractVideoConnector
	{
		public eCodecInputType CodecInputType { get; set; }
	}
}
