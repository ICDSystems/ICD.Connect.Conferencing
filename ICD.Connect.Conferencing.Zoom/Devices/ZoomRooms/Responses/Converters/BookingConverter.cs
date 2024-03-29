﻿#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using System;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.Bookings;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters
{
	public sealed class BookingConverter : AbstractGenericJsonConverter<Booking>
	{
		private const string ATTR_MEETING_NAME = "meetingName";
		private const string ATTR_START_TIME = "startTime";
		private const string ATTR_END_TIME = "endTime";
		private const string ATTR_CREATOR_NAME = "creatorName";
		private const string ATTR_CREATOR_EMAIL = "creatorEmail";
		private const string ATTR_MEETING_NUMBER = "meetingNumber";
		private const string ATTR_IS_PRIVATE = "isPrivate";
		private const string ATTR_HOST_NAME = "hostName";
		private const string ATTR_CHECK_IN = "checkIn";
		private const string ATTR_CHECKED_IN = "checkedIn";

		protected override void WriteProperties(JsonWriter writer, Booking value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.MeetingName != null)
				writer.WriteProperty(ATTR_MEETING_NAME, value.MeetingName);

			if (value.StartTime != default(DateTime))
				writer.WriteProperty(ATTR_START_TIME, value.StartTime);

			if (value.EndTime != default(DateTime))
				writer.WriteProperty(ATTR_END_TIME, value.EndTime);

			if (value.OrganizerName != null)
				writer.WriteProperty(ATTR_CREATOR_NAME, value.OrganizerName);

			if (value.OrganizerEmail != null)
				writer.WriteProperty(ATTR_CREATOR_EMAIL, value.OrganizerEmail);

			if (value.MeetingNumber != null)
				writer.WriteProperty(ATTR_MEETING_NUMBER, value.MeetingNumber);

			if (value.IsPrivate)
				writer.WriteProperty(ATTR_IS_PRIVATE, value.IsPrivate);

			if (value.HostName != null)
				writer.WriteProperty(ATTR_HOST_NAME, value.HostName);

			if (value.CheckedIn)
				writer.WriteProperty(ATTR_CHECKED_IN, value.CheckedIn);
		}

		protected override void ReadProperty(string property, JsonReader reader, Booking instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_MEETING_NAME:
					instance.MeetingName = reader.GetValueAsString();
					break;
				case ATTR_START_TIME:
					instance.StartTime = GetValueAsDateTime(reader, IcdEnvironment.GetLocalTime().Date).ToUniversalTime();
					break;
				case ATTR_END_TIME:
					instance.EndTime = GetValueAsDateTime(reader, (IcdEnvironment.GetLocalTime().Date + TimeSpan.FromDays(1))).ToUniversalTime();
					break;
				case ATTR_CREATOR_NAME:
					instance.OrganizerName = reader.GetValueAsString();
					break;
				case ATTR_CREATOR_EMAIL:
					instance.OrganizerEmail = reader.GetValueAsString();
					break;
				case ATTR_MEETING_NUMBER:
					string meetingNumber = reader.GetValueAsString();
					instance.MeetingNumber = meetingNumber == "0" ? null : meetingNumber;
					break;
				case ATTR_IS_PRIVATE:
					instance.IsPrivate = reader.GetValueAsBool();
					break;
				case ATTR_HOST_NAME:
					instance.HostName = reader.GetValueAsString();
					break;
				//Zoom's checkIn JSON attribute changes based on value so check for both.
				case ATTR_CHECK_IN:
				case ATTR_CHECKED_IN:
					instance.CheckedIn = reader.GetValueAsBool();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}

		/// <summary>
		/// Zoom reports all-day meetings by omitting the start and end times.
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		private DateTime GetValueAsDateTime(JsonReader reader, DateTime defaultValue)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");

			string value = reader.GetValueAsString();
			if (string.IsNullOrEmpty(value))
				return defaultValue;

			return reader.GetValueAsDateTime();
		}
	}
}
