using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Settings;

namespace ICD.Connect.Conferencing.ConferencePoints
{
	public interface IConferencePointSettings : ISettings
	{
		int DeviceId { get; set; }

		int ControlId { get; set; }

		eCallType Type { get; set; }
	}
}
