using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Components.Call
{
	public sealed class IncomingCall
	{
		/// <summary>
		/// Join ID of the caller. Used to accept the caller using <code>zCommand Call Accept</code>.
		/// </summary>
		[JsonProperty("callerJID")]
		public string CallerJoinId { get; private set; }

		/// <summary>
		/// Join ID of the host of the meeting being called
		/// </summary>
		[JsonProperty("calleeJID")]
		public string CalleeJoinId { get; private set; }

		/// <summary>
		/// Internal meeting ID used to private set up recording.
		/// </summary>
		[JsonProperty("meetingID")]
		public string MeetingId { get; private set; }

		/// <summary>
		/// The password entered by the participant who intends to join.
		/// </summary>
		[JsonProperty("password")]
		public string Password { get; private set; }

		/// <summary>
		/// ???
		/// </summary>
		[JsonProperty("meetingOption")]
		public int MeetingOption { get; private set; }

		/// <summary>
		/// Meeting number for this meeting
		/// </summary>
		[JsonProperty("meetingNumber")]
		public int MeetingNumber { get; private set; }

		[JsonProperty("callerName")]
		public string CallerName { get; private set; }

		/// <summary>
		/// Avatar image of the person joining
		/// </summary>
		[JsonProperty("avatarURL")]
		public string AvatarUrl { get; private set; }

		/// <summary>
		/// ???
		/// </summary>
		[JsonProperty("lifeTime")]
		public string Lifetime { get; private set; }
	}
}