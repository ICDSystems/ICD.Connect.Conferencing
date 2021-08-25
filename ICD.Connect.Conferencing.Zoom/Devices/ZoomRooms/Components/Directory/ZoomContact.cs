#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using System;
using System.Collections.Generic;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Contacts;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.Directory
{
	[JsonConverter(typeof(ZoomContactConverter))]
	public sealed class ZoomContact : IContactWithSurname, IContactWithOnlineState
	{
		public event EventHandler<OnlineStateEventArgs> OnOnlineStateChanged;

		private eContactPresence m_Presence;

		/// <summary>
		/// Use this ID when inviting the user, or when accepting / rejecting a user who is joining the conversation
		/// </summary>
		public string JoinId { get; set; }

		public string ScreenName { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }

		/// <summary>
		/// Phone number of the user (typically empty)
		/// </summary>
		public string PhoneNumber { get; set; }

		public string Email { get; set; }

		/// <summary>
		/// URL pointing to the image of the user
		/// </summary>
		public string AvatarUrl { get; set; }

		/// <summary>
		/// State of the Phonebook contact. PRESENCE_BUSY means "in a meeting", and PRESENCE_DND means "do not disturb"
		/// </summary>
		public eContactPresence Presence
		{
			get { return m_Presence; }
			set
			{
				if (value == m_Presence)
					return;

				m_Presence = value;

				OnOnlineStateChanged.Raise(this, new OnlineStateEventArgs(OnlineState));
			}
		}

		public bool IsZoomRoom { get; set; }

		/// <summary>
		/// Index of user in the phonebook, I think?
		/// Returned for an Updated Contact response, but not a Phonebook List response.
		/// </summary>
		public int? Index { get; set; }

		public string Name
		{
			get { return string.Format("{0} {1}", FirstName, LastName); }
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

		public IEnumerable<IDialContext> GetDialContexts()
		{
			yield return new DialContext {Protocol = eDialProtocol.ZoomContact, DialString = JoinId};
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
			Presence = contact.Presence;
		}
	}
}