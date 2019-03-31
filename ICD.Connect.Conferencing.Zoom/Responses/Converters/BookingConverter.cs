using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using ICD.Connect.Conferencing.Zoom.Components.Bookings;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses.Converters
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

		protected override void ReadProperty(string property, JsonReader reader, Booking instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_MEETING_NAME:
					instance.MeetingName = reader.GetValueAsString();
					break;
				case ATTR_START_TIME:
					instance.StartTime = reader.GetValueAsDateTime();
					break;
				case ATTR_END_TIME:
					instance.EndTime = reader.GetValueAsDateTime();
					break;
				case ATTR_CREATOR_NAME:
					instance.OrganizerName = reader.GetValueAsString();
					break;
				case ATTR_CREATOR_EMAIL:
					instance.OrganizerEmail = reader.GetValueAsString();
					break;
				case ATTR_MEETING_NUMBER:
					instance.MeetingNumber = reader.GetValueAsString();
					break;
				case ATTR_IS_PRIVATE:
					instance.IsPrivate = reader.GetValueAsBool();
					break;
				case ATTR_HOST_NAME:
					instance.HostName = reader.GetValueAsString();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}
