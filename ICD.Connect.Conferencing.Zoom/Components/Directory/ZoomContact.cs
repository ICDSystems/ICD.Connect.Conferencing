﻿using System;
using System.Collections.Generic;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Contacts;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Zoom.Responses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ICD.Connect.Conferencing.Zoom.Components.Directory
{
	public sealed class ZoomContact : AbstractZoomRoomData, IContactWithSurname, IContactWithOnlineState
	{
		public event EventHandler<OnlineStateEventArgs> OnOnlineStateChanged;
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

		/// <summary>
		/// URL pointing to the image of the user
		/// </summary>
		[JsonProperty("avatarURL")]
		public string AvatarUrl { get; private set; }

		/// <summary>
		/// State of the Phonebook contact. PRESENCE_BUSY means "in a meeting", and PRESENCE_DND means "do not disturb"
		/// </summary>
		[JsonProperty("presence")]
		public eContactPresence Presence { get; private set; }

		[JsonProperty("isZoomRoom")]
		public bool IsZoomRoom { get; private set; }

		/// <summary>
		/// Index of user in the phonebook, I think?
		/// Returned for an Updated Contact response, but not a Phonebook List response.
		/// </summary>
		[JsonProperty("index")]
		public int? Index { get; private set; }

		public string Name
		{
			get { return string.Format("{0} {1}", FirstName, LastName); }
		}

		public IEnumerable<IDialContext> GetDialContexts()
		{
			yield return new ZoomContactDialContext { DialString = JoinId };
		}
		
		public eOnlineState OnlineState
		{
			get
			{
				switch (Presence)
				{
					case eContactPresence.PRESENCE_OFFLINE:
						return eOnlineState.Offline;
					case eContactPresence.PRESENCE_ONLINE:
						return eOnlineState.Online;
					case eContactPresence.PRESENCE_BUSY:
						return eOnlineState.Busy;
					case eContactPresence.PRESENCE_AWAY:
						return eOnlineState.Away;
					case eContactPresence.PRESENCE_DND:
						return eOnlineState.DoNotDisturb;
					default:
						return eOnlineState.Unknown;
				}
			}
		}

		public void Update(ZoomContact contact)
		{
			FirstName = contact.FirstName;
			LastName = contact.LastName;
			JoinId = contact.JoinId;
			ScreenName = contact.ScreenName;
			AvatarUrl = contact.AvatarUrl;
			Email = contact.Email;
			Index = contact.Index;
			IsZoomRoom = contact.IsZoomRoom;
			PhoneNumber = contact.PhoneNumber;

			var oldPresence = Presence;
			Presence = contact.Presence;
			if (oldPresence != Presence)
				OnOnlineStateChanged.Raise(this, new OnlineStateEventArgs(OnlineState));
		}

		public override void LoadFromJObject(JObject jObject)
		{
			FirstName = jObject["firstName"].ToString();
			LastName = jObject["lastName"].ToString();
			JoinId = jObject["jid"].ToString();
			ScreenName = jObject["screenName"].ToString();
			AvatarUrl = jObject["avatarURL"].ToString();
			Email = jObject["email"].ToString();
			Index = jObject["index"] == null ? (int?)null : jObject["index"].ToObject<int>();
			PhoneNumber = jObject["phoneNumber"].ToString();
			IsZoomRoom = jObject["isZoomRoom"].ToObject<bool>();
			Presence = jObject["presence"].ToObject<eContactPresence>();
		}
	}
}