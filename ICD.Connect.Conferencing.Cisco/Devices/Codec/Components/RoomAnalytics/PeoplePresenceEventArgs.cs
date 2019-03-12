using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.RoomAnalytics
{
	public enum ePeoplePresence
	{
		Unknown,
		No,
		Yes
	}

	public sealed class PeoplePresenceEventArgs : GenericEventArgs<ePeoplePresence>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public PeoplePresenceEventArgs(ePeoplePresence data)
			: base(data)
		{
		}
	}
}
