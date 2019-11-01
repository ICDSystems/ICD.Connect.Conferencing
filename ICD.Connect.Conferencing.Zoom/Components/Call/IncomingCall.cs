using ICD.Connect.Conferencing.Zoom.Responses.Converters;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Components.Call
{
	[JsonConverter(typeof(IncomingCallConverter))]
	public sealed class IncomingCall
	{
		/// <summary>
		/// Join ID of the caller. Used to accept the caller using <code>zCommand Call Accept</code>.
		/// </summary>
		public string CallerJoinId { get; set; }

		/// <summary>
		/// Join ID of the host of the meeting being called
		/// </summary>
		public string CalleeJoinId { get; set; }

		/// <summary>
		/// Internal meeting ID used to private set up recording.
		/// </summary>
		public string MeetingId { get; set; }

		/// <summary>
		/// The password entered by the participant who intends to join.
		/// </summary>
		public string Password { get; set; }

		/// <summary>
		/// ???
		/// </summary>
		public int MeetingOption { get; set; }

		/// <summary>
		/// Meeting number for this meeting
		/// </summary>
		public string MeetingNumber { get; set; }

		/// <summary>
		/// The name of the person calling.
		/// </summary>
		public string CallerName { get; set; }

		/// <summary>
		/// Avatar image of the person joining
		/// </summary>
		public string AvatarUrl { get; set; }

		/// <summary>
		/// Scheduled duration of the meeting
		/// </summary>
		public string Lifetime { get; set; }
	}
}