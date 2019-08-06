using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Devices.Points;

namespace ICD.Connect.Conferencing.ConferencePoints
{
	public interface IConferencePoint : IPoint
	{
		eCallType Type { get; set; }
	}
}