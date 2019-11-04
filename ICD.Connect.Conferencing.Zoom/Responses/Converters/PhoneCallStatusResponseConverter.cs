using System;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses.Converters
{
	public sealed class PhoneCallStatusResponseConverter : AbstractZoomRoomResponseConverter<PhoneCallStatusResponse>
	{
		private const string ATTR_PHONE_CALL_STATUS = "PhoneCallStatus";

		protected override void WriteProperties(JsonWriter writer, PhoneCallStatusResponse value,
		                                        JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.PhoneCallStatus != null)
			{
				writer.WritePropertyName(ATTR_PHONE_CALL_STATUS);
				serializer.Serialize(writer, value.PhoneCallStatus);
			}
		}

		protected override void ReadProperty(string property, JsonReader reader, PhoneCallStatusResponse instance,
		                                     JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_PHONE_CALL_STATUS:
					instance.PhoneCallStatus = serializer.Deserialize<PhoneCallStatus>(reader);
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}

	public sealed class PhoneCallStatusConverter : AbstractGenericJsonConverter<PhoneCallStatus>
	{
		private const string ATTR_CALL_ID = "callID";
		private const string ATTR_IS_INCOMING_CALL = "isIncomingCall";
		private const string ATTR_PEER_DISPLAY_NAME = "peerDisplayName";
		private const string ATTR_PEER_NUMBER = "peerNumber";
		private const string ATTR_PEER_URI = "peerUri";
		private const string ATTR_STATUS = "status";

		protected override void WriteProperties(JsonWriter writer, PhoneCallStatus value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.CallId != null)
				serializer.Serialize(writer, value.CallId);

			if (value.IsIncomingCall)
				serializer.Serialize(writer, value.IsIncomingCall);

			if (value.PeerDisplayName != null)
				serializer.Serialize(writer, value.PeerDisplayName);

			if (value.PeerNumber != null)
				serializer.Serialize(writer, value.PeerNumber);

			if (value.PeerUri != null)
				serializer.Serialize(writer, value.PeerUri);

			if (value.Status != 0)
				serializer.Serialize(writer, value.Status);
		}

		protected override void ReadProperty(string property, JsonReader reader, PhoneCallStatus instance,
		                                     JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_CALL_ID:
					instance.CallId = reader.GetValueAsString();
					break;
				case ATTR_IS_INCOMING_CALL:
					instance.IsIncomingCall = reader.GetValueAsBool();
					break;
				case ATTR_PEER_DISPLAY_NAME:
					instance.PeerDisplayName = reader.GetValueAsString();
					break;
				case ATTR_PEER_NUMBER:
					instance.PeerNumber = reader.GetValueAsString();
					break;
				case ATTR_PEER_URI:
					instance.PeerUri = reader.GetValueAsString();
					break;
				case ATTR_STATUS:
					instance.Status = serializer.Deserialize<eZoomPhoneCallStatus>(reader);
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}

	public sealed class ZoomPhoneCallStatusConverter : JsonConverter
	{
		private const string ATTR_RINGING = "PhoneCallStatus_Rining";
		private const string ATTR_INIT = "PhoneCallStatus_Init";
		private const string ATTR_IN_CALL = "PhoneCallStatus_InCall";
		private const string ATTR_INCOMING = "PhoneCallStatus_Incoming";
		private const string ATTR_NOT_FOUND = "PhoneCallStatus_NotFound";

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			eZoomPhoneCallStatus status = (eZoomPhoneCallStatus)value;

			switch (status)
			{
				case eZoomPhoneCallStatus.Ringing:
					writer.WriteValue(ATTR_RINGING);
					break;
				case eZoomPhoneCallStatus.Init:
					writer.WriteValue(ATTR_INIT);
					break;
				case eZoomPhoneCallStatus.InCall:
					writer.WriteValue(ATTR_IN_CALL);
					break;
				case eZoomPhoneCallStatus.Incoming:
					writer.WriteValue(ATTR_INCOMING);
					break;
				case eZoomPhoneCallStatus.NotFound:
					writer.WriteValue(ATTR_NOT_FOUND);
					break;
			}
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			string value = reader.GetValueAsString();

			switch (value)
			{
				case ATTR_RINGING:
					return eZoomPhoneCallStatus.Ringing;
				case ATTR_INIT:
					return eZoomPhoneCallStatus.Init;
				case ATTR_IN_CALL:
					return eZoomPhoneCallStatus.InCall;
				case ATTR_INCOMING:
					return eZoomPhoneCallStatus.Incoming;
				case ATTR_NOT_FOUND:
					return eZoomPhoneCallStatus.NotFound;

				default:
					return eZoomPhoneCallStatus.None;
			}
		}

		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(eZoomPhoneCallStatus);
		}
	}

	public sealed class PhoneCallTerminatedResponseConverter : AbstractZoomRoomResponseConverter<PhoneCallTerminatedResponse>
	{
		private const string ATTR_PHONE_CALL_TERMINATED = "PhoneCallTerminated";

		protected override void WriteProperties(JsonWriter writer, PhoneCallTerminatedResponse value,
												JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.PhoneCallTerminated != null)
			{
				writer.WritePropertyName(ATTR_PHONE_CALL_TERMINATED);
				serializer.Serialize(writer, value.PhoneCallTerminated);
			}
		}

		protected override void ReadProperty(string property, JsonReader reader, PhoneCallTerminatedResponse instance,
											 JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_PHONE_CALL_TERMINATED:
					instance.PhoneCallTerminated = serializer.Deserialize<PhoneCallTerminated>(reader);
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}

	public sealed class PhoneCallTerminatedConverter : AbstractGenericJsonConverter<PhoneCallTerminated>
	{
		private const string ATTR_CALL_ID = "callID";
		private const string ATTR_IS_INCOMING_CALL = "isIncomingCall";
		private const string ATTR_PEER_DISPLAY_NAME = "peerDisplayName";
		private const string ATTR_PEER_NUMBER = "peerNumber";
		private const string ATTR_PEER_URI = "peerUri";
		private const string ATTR_REASON = "reason";

		protected override void WriteProperties(JsonWriter writer, PhoneCallTerminated value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.CallId != null)
				serializer.Serialize(writer, value.CallId);

			if (value.IsIncomingCall)
				serializer.Serialize(writer, value.IsIncomingCall);

			if (value.PeerDisplayName != null)
				serializer.Serialize(writer, value.PeerDisplayName);

			if (value.PeerNumber != null)
				serializer.Serialize(writer, value.PeerNumber);

			if (value.PeerUri != null)
				serializer.Serialize(writer, value.PeerUri);

			if (value.Reason != 0)
				serializer.Serialize(writer, value.Reason);
		}

		protected override void ReadProperty(string property, JsonReader reader, PhoneCallTerminated instance,
											 JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_CALL_ID:
					instance.CallId = reader.GetValueAsString();
					break;
				case ATTR_IS_INCOMING_CALL:
					instance.IsIncomingCall = reader.GetValueAsBool();
					break;
				case ATTR_PEER_DISPLAY_NAME:
					instance.PeerDisplayName = reader.GetValueAsString();
					break;
				case ATTR_PEER_NUMBER:
					instance.PeerNumber = reader.GetValueAsString();
					break;
				case ATTR_PEER_URI:
					instance.PeerUri = reader.GetValueAsString();
					break;
				case ATTR_REASON:
					instance.Reason = serializer.Deserialize<eZoomPhoneCallTerminatedReason>(reader);
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}

	public sealed class ZoomPhoneCallTerminatedReasonConverter : JsonConverter
	{
		private const string ATTR_BY_LOCAL = "PhoneCallTerminateReason_ByLocal";
		private const string ATTR_BY_REMOTE = "PhoneCallTerminateReason_ByRemote";

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			eZoomPhoneCallTerminatedReason status = (eZoomPhoneCallTerminatedReason)value;

			switch (status)
			{
				case eZoomPhoneCallTerminatedReason.ByLocal:
					writer.WriteValue(ATTR_BY_LOCAL);
					break;
				case eZoomPhoneCallTerminatedReason.ByRemote:
					writer.WriteValue(ATTR_BY_REMOTE);
					break;
			}
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			string value = reader.GetValueAsString();

			switch (value)
			{
				case ATTR_BY_LOCAL:
					return eZoomPhoneCallTerminatedReason.ByLocal;
				case ATTR_BY_REMOTE:
					return eZoomPhoneCallTerminatedReason.ByRemote;

				default:
					return eZoomPhoneCallTerminatedReason.None;
			}
		}

		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(eZoomPhoneCallTerminatedReason);
		}
	}
}
