using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses.Converters
{
	public sealed class CallConfigurationResponseConverter : AbstractGenericJsonConverter<CallConfigurationResponse>
	{
		private const string ATTR_CALL = "Call";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, CallConfigurationResponse value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.CallConfiguration != null)
			{
				writer.WritePropertyName(ATTR_CALL);
				serializer.Serialize(writer, value.CallConfiguration);
			}
		}

		protected override void ReadProperty(string property, JsonReader reader, CallConfigurationResponse instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_CALL:
					instance.CallConfiguration = serializer.Deserialize<CallConfiguration>(reader);
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}

	public sealed class CallConfigurationConverter : AbstractGenericJsonConverter<CallConfiguration>
	{
		private const string ATTR_MICROPHONE = "Microphone";
		private const string ATTR_CAMERA = "Camera";
		private const string ATTR_LOCK = "Lock";
		private const string ATTR_LAYOUT = "Layout";
		private const string ATTR_MUTE_USER_ON_ENTRY = "MuteUserOnEntry";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, CallConfiguration value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.Microphone != null)
			{
				writer.WritePropertyName(ATTR_MICROPHONE);
				serializer.Serialize(writer, value.Microphone);
			}

			if (value.Camera != null)
			{
				writer.WritePropertyName(ATTR_CAMERA);
				serializer.Serialize(writer, value.Camera);
			}

			if (value.CallLockStatus != null)
			{
				writer.WritePropertyName(ATTR_LOCK);
				serializer.Serialize(writer, value.CallLockStatus);
			}

			if (value.Layout != null)
			{
				writer.WritePropertyName(ATTR_LAYOUT);
				serializer.Serialize(writer, value.Layout);
			}

			if (value.MuteUserOnEntry != null)
			{
				writer.WritePropertyName(ATTR_MUTE_USER_ON_ENTRY);
				serializer.Serialize(writer, value.MuteUserOnEntry);
			}
		}

		protected override void ReadProperty(string property, JsonReader reader, CallConfiguration instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_MICROPHONE:
					instance.Microphone = serializer.Deserialize<MuteConfiguration>(reader);
					break;

				case ATTR_CAMERA:
					instance.Camera = serializer.Deserialize<MuteConfiguration>(reader);
					break;

				case ATTR_LOCK:
					instance.CallLockStatus = serializer.Deserialize<LockConfiguration>(reader);
					break;

				case ATTR_LAYOUT:
					instance.Layout = serializer.Deserialize<CallLayoutConfigurationQuery>(reader);
					break;

				case ATTR_MUTE_USER_ON_ENTRY:
					instance.MuteUserOnEntry = serializer.Deserialize<MuteUserOnEntryConfiguration>(reader);
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}

	public sealed class MuteConfigurationConverter : AbstractGenericJsonConverter<MuteConfiguration>
	{
		private const string ATTR_MUTE = "Mute";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, MuteConfiguration value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.Mute)
				writer.WriteProperty(ATTR_MUTE, value.Mute);
		}

		protected override void ReadProperty(string property, JsonReader reader, MuteConfiguration instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_MUTE:
					instance.Mute = reader.GetValueAsBool();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}

	public sealed class LockConfigurationConverter : AbstractGenericJsonConverter<LockConfiguration>
	{
		private const string ATTR_LOCK = "Enable";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, LockConfiguration value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value != null)
				writer.WriteProperty(ATTR_LOCK, value.Lock);
		}

		protected override void ReadProperty(string property, JsonReader reader, LockConfiguration instance,
		                                     JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_LOCK:
					instance.Lock = reader.GetValueAsBool();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}

	public sealed class CallLayoutConfigurationQueryConverter : AbstractGenericJsonConverter<CallLayoutConfigurationQuery>
	{
		private const string ATTR_SIZE = "Size";
		private const string ATTR_POSITION = "Position";

		protected override void WriteProperties(JsonWriter writer, CallLayoutConfigurationQuery value,
		                                        JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.Size != null)
				writer.WriteProperty(ATTR_SIZE, value.Size);

			if (value.Position != null)
				writer.WriteProperty(ATTR_POSITION, value.Position);
		}

		protected override void ReadProperty(string property, JsonReader reader, CallLayoutConfigurationQuery instance,
		                                     JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_SIZE:
					instance.Size = reader.GetValueAsEnum<eZoomLayoutSize>();
					break;

				case ATTR_POSITION:
					instance.Position = reader.GetValueAsEnum<eZoomLayoutPosition>();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}

	public sealed class MuteUserOnEntryConfigurationConverter : AbstractGenericJsonConverter<MuteUserOnEntryConfiguration>
	{
		private const string ATTR_ENABLE = "Enable";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, MuteUserOnEntryConfiguration value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value != null)
				writer.WriteProperty(ATTR_ENABLE, value.Enabled);
		}

		protected override void ReadProperty(string property, JsonReader reader, MuteUserOnEntryConfiguration instance,
		                                     JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_ENABLE:
					instance.Enabled = reader.GetValueAsBool();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}
