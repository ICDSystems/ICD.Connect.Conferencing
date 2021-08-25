#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.Call;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters
{
	public sealed class IncomingCallConverter : AbstractGenericJsonConverter<IncomingCall>
	{
		private const string ATTR_CALLER_JOIN_ID ="callerJID";
		private const string ATTR_CALLEE_JOIN_ID ="calleeJID";
		private const string ATTR_MEETING_ID ="meetingID";
		private const string ATTR_PASSWORD ="password";
		private const string ATTR_MEETING_OPTION ="meetingOption";
		private const string ATTR_MEETING_NUMBER ="meetingNumber";
		private const string ATTR_CALLER_NAME ="callerName";
		private const string ATTR_AVATAR_URL ="avatarURL";
		private const string ATTR_LIFE_TIME = "lifeTime";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, IncomingCall value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.CallerJoinId != null)
				writer.WriteProperty(ATTR_CALLER_JOIN_ID, value.CallerJoinId);

			if (value.MeetingNumber != null)
				writer.WriteProperty(ATTR_MEETING_NUMBER, value.MeetingNumber);

			if (value.CallerName != null)
				writer.WriteProperty(ATTR_CALLER_NAME, value.CallerName);
		}

		protected override void ReadProperty(string property, JsonReader reader, IncomingCall instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_CALLER_JOIN_ID:
					instance.CallerJoinId = reader.GetValueAsString();
					break;
				case ATTR_MEETING_NUMBER:
					instance.MeetingNumber = reader.GetValueAsString();
					break;
				case ATTR_CALLER_NAME:
					instance.CallerName = reader.GetValueAsString();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}
