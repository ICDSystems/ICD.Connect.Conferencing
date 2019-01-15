using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Settings.Originators;

namespace ICD.Connect.Conferencing.ConferencePoints
{
	public interface IConferencePoint : IOriginator
	{
		/// <summary>
		/// Device id
		/// </summary>
		int DeviceId { get; set; }

		/// <summary>
		/// Control id.
		/// </summary>
		int ControlId { get; set; }

		eCallType Type { get; set; }
	}
}