#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters
{
	public sealed class UpdatedCallRecordInfoResponseConverter : AbstractZoomRoomResponseConverter<UpdatedCallRecordInfoResponse>
	{
		private const string ATTR_UPDATE_CALL_RECORD_INFO = "UpdateCallRecordInfo";

		protected override void WriteProperties(JsonWriter writer, UpdatedCallRecordInfoResponse value,
		                                        JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.CallRecordInfo != null)
			{
				writer.WritePropertyName(ATTR_UPDATE_CALL_RECORD_INFO);
				serializer.Serialize(writer,value.CallRecordInfo);
			}
		}

		protected override void ReadProperty(string property, JsonReader reader, UpdatedCallRecordInfoResponse instance,
		                                     JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_UPDATE_CALL_RECORD_INFO:
					instance.CallRecordInfo = serializer.Deserialize<UpdateCallRecordInfoEvent>(reader);
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}

	public sealed class UpdatedCallRecordInfoEventConverter : AbstractGenericJsonConverter<UpdateCallRecordInfoEvent>
	{
		private const string ATTR_CAN_RECORD = "canRecord";
		private const string ATTR_EMAIL_REQUIRED = "emailRequired";
		private const string ATTR_AM_I_RECORDING = "amIRecording";
		private const string ATTR_MEETING_IS_BEING_RECORDED = "meetingIsBeingRecorded";

		protected override void WriteProperties(JsonWriter writer, UpdateCallRecordInfoEvent value,
		                                        JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.CanRecord)
				writer.WriteProperty(ATTR_CAN_RECORD, value.CanRecord);

			if (value.EmailRequired)
				writer.WriteProperty(ATTR_EMAIL_REQUIRED, value.EmailRequired);

			if (value.AmIRecording)
				writer.WriteProperty(ATTR_AM_I_RECORDING, value.AmIRecording);

			if (value.MeetingsIsBeingRecorded)
				writer.WriteProperty(ATTR_MEETING_IS_BEING_RECORDED, value.MeetingsIsBeingRecorded);
		}

		protected override void ReadProperty(string property, JsonReader reader, UpdateCallRecordInfoEvent instance,
		                                     JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_CAN_RECORD:
					instance.CanRecord = reader.GetValueAsBool();
					break;
				case ATTR_EMAIL_REQUIRED:
					instance.EmailRequired = reader.GetValueAsBool() ;
					break;
				case ATTR_AM_I_RECORDING:
					instance.AmIRecording = reader.GetValueAsBool();
					break;
				case ATTR_MEETING_IS_BEING_RECORDED:
					instance.MeetingsIsBeingRecorded = reader.GetValueAsBool();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}
