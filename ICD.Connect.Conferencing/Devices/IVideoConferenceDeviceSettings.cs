using ICD.Connect.Devices;

namespace ICD.Connect.Conferencing.Devices
{
	public interface IVideoConferenceDeviceSettings : IDeviceSettings
	{
		eCodecInputType Input1CodecInputType { get; set; }
		eCodecInputType Input2CodecInputType { get; set; }
		eCodecInputType Input3CodecInputType { get; set; }
		eCodecInputType Input4CodecInputType { get; set; }
		eCodecInputType Input5CodecInputType { get; set; }
		eCodecInputType Input6CodecInputType { get; set; }

		int? DefaultCameraDevice { get; set; }
	}
}
