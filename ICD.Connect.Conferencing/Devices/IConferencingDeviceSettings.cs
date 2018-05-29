using ICD.Connect.Devices;

namespace ICD.Connect.Conferencing.Devices
{
	public interface IConferencingDeviceSettings : IDeviceSettings
	{
		eCodecInputType Input1CodecInputType { get; set; }
		eCodecInputType Input2CodecInputType { get; set; }
		eCodecInputType Input3CodecInputType { get; set; }
		eCodecInputType Input4CodecInputType { get; set; }
	}
}
