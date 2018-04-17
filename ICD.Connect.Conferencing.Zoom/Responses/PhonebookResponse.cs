﻿using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("Phonebook", eZoomRoomApiType.zEvent, false)]
	public sealed class PhonebookContactUpdatedResponse : AbstractZoomRoomResponse
	{
		[JsonProperty("Phonebook")]
		public PhonebookUpdatedContact Data { get; private set; }
	}

	[ZoomRoomApiResponse("PhonebookListResponse", eZoomRoomApiType.zCommand, true)]
	public sealed class PhonebookListCommandResponse : AbstractZoomRoomResponse
	{
		[JsonProperty("PhonebookListResult")]
		public PhonebookListResult PhonebookListResult { get; private set; }
	}

	public sealed class PhonebookListResult
	{
		[JsonProperty("Contacts")]
		public ZoomContact[] Contacts { get; private set; }

		[JsonProperty("Limit")]
		public int Limit { get; private set; }

		[JsonProperty("Offset")]
		public int Offset { get; private set; }
	}

	public sealed class PhonebookUpdatedContact
	{
		[JsonProperty("Updated Contact")]
		public ZoomContact Contact { get; private set; }
	}

	public sealed class ZoomContact
	{
		/// <summary>
		/// Use this ID when inviting the user, or when accepting / rejecting a user who is joining the conversation
		/// </summary>
		[JsonProperty("jid")]
		public string JoinId { get; private set; }

		[JsonProperty("screenName")]
		public string ScreenName { get; private set; }

		[JsonProperty("firstName")]
		public string FirstName { get; private set; }

		[JsonProperty("lastName")]
		public string LastName { get; private set; }

		/// <summary>
		/// Phone number of the user (typically empty)
		/// </summary>
		[JsonProperty("phoneNumber")]
		public string PhoneNumber { get; private set; }

		[JsonProperty("email")]
		public string Email { get; private set; }

		[JsonProperty("avatarURL")]
		public string AvatarUrl { get; private set; }

		[JsonProperty("presence")]
		public eContactPresence Presence { get; private set; }
	}

	public enum eContactPresence
	{
		PRESENCE_OFFLINE,
		PRESENCE_ONLINE,
		PRESENCE_AWAY,
		PRESENCE_DND,
		PRESENCE_BUSY
	}
}