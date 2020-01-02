using ICD.Common.Properties;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Devices.Points;

namespace ICD.Connect.Conferencing.ConferencePoints
{
	public interface IConferencePoint : IPoint
	{
		/// <summary>
		/// Gets/sets the control for this point.
		/// </summary>
		[CanBeNull]
		new IConferenceDeviceControl Control { get; }

		/// <summary>
		/// The type of call to use the conference control for.
		/// </summary>
		eCallType Type { get; set; }
	}
}