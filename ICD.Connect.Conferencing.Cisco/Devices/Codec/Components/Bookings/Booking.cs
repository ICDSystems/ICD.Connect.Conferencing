﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Xml;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Bookings
{
	public sealed class Booking
	{
		// 2018-09-04T04:00:00Z
		private const string DATE_FORMAT = "yyyy-MM-ddTHH:mm:ssZ";

		public enum ePrivacy
		{
			Public,
			Private
		}

		private readonly IcdOrderedDictionary<int, DialInfo> m_DialInfos;

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
			m_DialInfos = new IcdOrderedDictionary<int, DialInfo>();
		}

		/// <summary>
		/// Deserializes the given xml to a DialInfo instance.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		public static Booking FromXml(string xml)
		{
			int id = XmlUtils.GetAttributeAsInt(xml, "id");
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
			IEnumerable<DialInfo> dialInfos = XmlUtils.GetChildElementsAsString(dialInfoXml).Select(x => DialInfo.FromXml(x));

			foreach (DialInfo info in dialInfos)
				booking.m_DialInfos.Add(info.Id, info);

			return booking;
		}

		/// <summary>
		/// Gets the dial info instances for this booking.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<DialInfo> GetDialInfos()
		{
			return m_DialInfos.Values.ToArray(m_DialInfos.Count);
		}

		/// <summary>
		/// Updates this booking with details from the given booking.
		/// </summary>
		/// <param name="booking"></param>
		/// <returns></returns>
		public bool Update(Booking booking)
		{
			throw new NotImplementedException();
		}
	}
}
