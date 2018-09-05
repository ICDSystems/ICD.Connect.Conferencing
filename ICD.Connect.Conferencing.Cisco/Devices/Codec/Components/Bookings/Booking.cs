using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Xml;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Bookings
{
	public sealed class Booking : IEquatable<Booking>
	{
		// 2018-09-04T04:00:00Z
		private const string DATE_FORMAT = @"yyyy-MM-dd\THH:mm:ss\Z";

		public enum ePrivacy
		{
			Public,
			Private
		}

		private readonly IcdOrderedDictionary<string, BookingCall> m_Calls;

		public int Id { get; private set; }
		public string Title { get; private set; }
		public string Agenda { get; private set; }
		public ePrivacy Privacy { get; private set; }
		public string OrganizerFirstName { get; private set; }
		public string OrganizerLastName { get; private set; }
		public string OrganizerEmail { get; private set; }
		public string OrganizerId { get; private set; }
		public DateTime StartTime { get; private set; }
		//public TimeSpan StartTimeBuffer { get; private set; }
		public DateTime EndTime { get; private set; }
		//public TimeSpan EndTimeBuffer { get; private set; }
		//public TimeSpan MaximumMeetingExtension { get; private set; }
		//public bool MeetingExtensionAvailability { get; private set; }
		//public eStatus BookingStatus { get; private set; }
		public string BookingStatusMessage { get; private set; }
		public bool WebexEnabled { get; private set; }
		public string WebexUrl { get; private set; }
		public string WebexMeetingNumber { get; private set; }
		public string WebexPassword { get; private set; }
		public string WebexHostKey { get; private set; }
		//public eEncryption Encryption { get; private set; }
		//public eRole Role { get; private set; }
		//public eRecording Recording { get; private set; }
		//public eConnectMode ConnectMode { get; private set; }

		/// <summary>
		/// Constructor.
		/// </summary>
		private Booking()
		{
			m_Calls = new IcdOrderedDictionary<string, BookingCall>();
		}

		/// <summary>
		/// Deserializes the given xml to a Booking instance.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		public static Booking FromXml(string xml)
		{
			int id = XmlUtils.TryReadChildElementContentAsInt(xml, "Id") ?? 0;
			string title = XmlUtils.TryReadChildElementContentAsString(xml, "Title");
			string agenda = XmlUtils.TryReadChildElementContentAsString(xml, "Agenda");
			ePrivacy privacy = XmlUtils.TryReadChildElementContentAsEnum<ePrivacy>(xml, "Privacy", true) ?? ePrivacy.Public;

			string organizerXml = XmlUtils.GetChildElementAsString(xml, "Organizer");

			string organizerFirstName = XmlUtils.TryReadChildElementContentAsString(organizerXml, "FirstName");
			string organizerLastName = XmlUtils.TryReadChildElementContentAsString(organizerXml, "LastName");
			string organizerEmail = XmlUtils.TryReadChildElementContentAsString(organizerXml, "Email");
			string organizerId = XmlUtils.TryReadChildElementContentAsString(organizerXml, "Id");

			string timeXml = XmlUtils.GetChildElementAsString(xml, "Time");

			string startTimeString = XmlUtils.TryReadChildElementContentAsString(timeXml, "StartTime");
			string endTimeString = XmlUtils.TryReadChildElementContentAsString(timeXml, "EndTime");

			DateTime startTime = DateTime.ParseExact(startTimeString, DATE_FORMAT, CultureInfo.InvariantCulture);
			DateTime endTime = DateTime.ParseExact(endTimeString, DATE_FORMAT, CultureInfo.InvariantCulture);

			string bookingStatusMessage = XmlUtils.TryReadChildElementContentAsString(xml, "BookingStatusMessage");

			string webexXml = XmlUtils.GetChildElementAsString(xml, "Webex");

			bool webexEnabled = XmlUtils.TryReadChildElementContentAsBoolean(webexXml, "Enabled") ?? false;
			string webexUrl = XmlUtils.TryReadChildElementContentAsString(webexXml, "Url");
			string webexMeetingNumber = XmlUtils.TryReadChildElementContentAsString(webexXml, "MeetingNumber");
			string webexPassword = XmlUtils.TryReadChildElementContentAsString(webexXml, "Password");
			string webexHostKey = XmlUtils.TryReadChildElementContentAsString(webexXml, "HostKey");

			Booking booking = new Booking
			{
				Id = id,
				Title = title,
				Agenda = agenda,
				Privacy = privacy,

				OrganizerEmail = organizerEmail,
				OrganizerFirstName = organizerFirstName,
				OrganizerLastName = organizerLastName,
				OrganizerId = organizerId,

				StartTime = startTime,
				EndTime = endTime,

				BookingStatusMessage = bookingStatusMessage,

				WebexEnabled = webexEnabled,
				WebexUrl = webexUrl,
				WebexMeetingNumber = webexMeetingNumber,
				WebexPassword = webexPassword,
				WebexHostKey = webexHostKey
			};

			string dialInfoXml = XmlUtils.GetChildElementAsString(xml, "DialInfo");
			string callsXml = XmlUtils.GetChildElementAsString(dialInfoXml, "Calls");
			IEnumerable<BookingCall> bookingCalls = XmlUtils.GetChildElementsAsString(callsXml).Select(x => BookingCall.FromXml(x));

			foreach (BookingCall info in bookingCalls)
				booking.m_Calls.Add(info.Number, info);

			return booking;
		}

		/// <summary>
		/// Gets the booking call instances for this booking.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<BookingCall> GetCalls()
		{
			return m_Calls.Values.ToArray(m_Calls.Count);
		}

		/// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
		/// <param name="other">An object to compare with this object.</param>
		/// <returns>true if the current object is equal to the <paramref name="other">other</paramref> parameter; otherwise, false.</returns>
		public bool Equals(Booking other)
		{
			if (ReferenceEquals(null, other))
				return false;

			if (ReferenceEquals(this, other))
				return true;

			return Id == other.Id &&
			       string.Equals(Title, other.Title) &&
			       string.Equals(Agenda, other.Agenda) &&
			       Privacy == other.Privacy &&
			       string.Equals(OrganizerFirstName, other.OrganizerFirstName) &&
			       string.Equals(OrganizerLastName, other.OrganizerLastName) &&
			       string.Equals(OrganizerEmail, other.OrganizerEmail) &&
			       string.Equals(OrganizerId, other.OrganizerId) &&
			       StartTime.Equals(other.StartTime) &&
			       EndTime.Equals(other.EndTime) &&
			       string.Equals(BookingStatusMessage, other.BookingStatusMessage) &&
			       WebexEnabled == other.WebexEnabled &&
			       string.Equals(WebexUrl, other.WebexUrl) &&
			       string.Equals(WebexMeetingNumber, other.WebexMeetingNumber) &&
			       string.Equals(WebexPassword, other.WebexPassword) &&
			       string.Equals(WebexHostKey, other.WebexHostKey) &&
			       m_Calls.DictionaryEqual(other.m_Calls);
		}

		/// <summary>Determines whether the specified object is equal to the current object.</summary>
		/// <param name="obj">The object to compare with the current object.</param>
		/// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			
			if (ReferenceEquals(this, obj))
				return true;
			
			return obj is Booking && Equals((Booking)obj);
		}

		/// <summary>Serves as the default hash function.</summary>
		/// <returns>A hash code for the current object.</returns>
		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = Id;
				hashCode = (hashCode * 397) ^ (Title != null ? Title.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (Agenda != null ? Agenda.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (int)Privacy;
				hashCode = (hashCode * 397) ^ (OrganizerFirstName != null ? OrganizerFirstName.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (OrganizerLastName != null ? OrganizerLastName.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (OrganizerEmail != null ? OrganizerEmail.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (OrganizerId != null ? OrganizerId.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ StartTime.GetHashCode();
				hashCode = (hashCode * 397) ^ EndTime.GetHashCode();
				hashCode = (hashCode * 397) ^ (BookingStatusMessage != null ? BookingStatusMessage.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ WebexEnabled.GetHashCode();
				hashCode = (hashCode * 397) ^ (WebexUrl != null ? WebexUrl.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (WebexMeetingNumber != null ? WebexMeetingNumber.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (WebexPassword != null ? WebexPassword.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (WebexHostKey != null ? WebexHostKey.GetHashCode() : 0);

				foreach (BookingCall item in m_Calls.Values)
					hashCode = (hashCode * 397) ^ item.GetHashCode();

				return hashCode;
			}
		}
	}
}
