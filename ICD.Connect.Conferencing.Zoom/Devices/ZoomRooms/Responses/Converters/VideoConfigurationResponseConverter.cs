﻿#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters
{
	public sealed class VideoConfigurationResponseConverter : AbstractZoomRoomResponseConverter<VideoConfigurationResponse>
	{
		private const string ATTR_VIDEO = "Video";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, VideoConfigurationResponse value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.Video != null)
			{
				writer.WritePropertyName(ATTR_VIDEO);
				serializer.Serialize(writer, value.Video);
			}
		}

		protected override void ReadProperty(string property, JsonReader reader, VideoConfigurationResponse instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_VIDEO:
					instance.Video = serializer.Deserialize<VideoConfiguration>(reader);
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}

	public sealed class VideoConfigurationConverter : AbstractGenericJsonConverter<VideoConfiguration>
	{
		private const string ATTR_CAMERA = "Camera";
		private const string ATTR_HIDE_CONF_SELF_VIDEO = "hide_conf_self_video";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, VideoConfiguration value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.Camera != null)
			{
				writer.WritePropertyName(ATTR_CAMERA);
				serializer.Serialize(writer, value.Camera);
			}

			if (value.HideConferenceSelfVideo)
			{
				writer.WritePropertyName(ATTR_HIDE_CONF_SELF_VIDEO);
				serializer.Serialize(writer, value.HideConferenceSelfVideo);
			}
		}

		protected override void ReadProperty(string property, JsonReader reader, VideoConfiguration instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_CAMERA:
					instance.Camera = serializer.Deserialize<VideoCameraConfiguration>(reader);
					break;
				case ATTR_HIDE_CONF_SELF_VIDEO:
					instance.HideConferenceSelfVideo = serializer.Deserialize<bool>(reader);
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}

	public sealed class VideoCameraConfigurationConverter : AbstractGenericJsonConverter<VideoCameraConfiguration>
	{
		private const string ATTR_SELECTED_ID = "selectedId";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, VideoCameraConfiguration value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.SelectedId != null)
			{
				writer.WritePropertyName(ATTR_SELECTED_ID);
				writer.WriteValue(value.SelectedId);
			}
		}

		protected override void ReadProperty(string property, JsonReader reader, VideoCameraConfiguration instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_SELECTED_ID:
					instance.SelectedId = reader.GetValueAsString();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}
