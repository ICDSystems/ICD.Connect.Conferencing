using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses.Converters
{

	public sealed class ClientCallLayoutResponseConverter : AbstractZoomRoomResponseConverter<ClientCallLayoutResponse>
	{
		private const string ATTR_CLIENT = "Client";

		protected override void WriteProperties(JsonWriter writer, ClientCallLayoutResponse value,
		                                        JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.CallLayoutConfiguration != null)
			{
				writer.WritePropertyName(ATTR_CLIENT);
				serializer.Serialize(writer, value.CallLayoutConfiguration);
			}
		}

		protected override void ReadProperty(string property, JsonReader reader, ClientCallLayoutResponse instance,
		                                     JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_CLIENT:
					instance.CallLayoutConfiguration = serializer.Deserialize<CallLayoutConfiguration>(reader);
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}

	public sealed class CallLayoutConfigurationConverter : AbstractGenericJsonConverter<CallLayoutConfiguration>
	{
		private const string ATTR_CALL = "Call";

		protected override void WriteProperties(JsonWriter writer, CallLayoutConfiguration value,
		                                        JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.LayoutConfigurationHeader != null)
			{
				writer.WritePropertyName(ATTR_CALL);
				serializer.Serialize(writer, value.LayoutConfigurationHeader);
			}
		}

		protected override void ReadProperty(string property, JsonReader reader, CallLayoutConfiguration instance,
		                                     JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_CALL:
					instance.LayoutConfigurationHeader = serializer.Deserialize<LayoutConfigurationHeader>(reader);
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}

	public sealed class LayoutConfigurationHeaderConverter : AbstractGenericJsonConverter<LayoutConfigurationHeader>
	{
		private const string ATTR_LAYOUT = "Layout";

		protected override void WriteProperties(JsonWriter writer, LayoutConfigurationHeader value,
		                                        JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.LayoutConfiguration != null)
			{
				writer.WritePropertyName("Layout");
				serializer.Serialize(writer, value.LayoutConfiguration);
			}
		}

		protected override void ReadProperty(string property, JsonReader reader, LayoutConfigurationHeader instance,
		                                     JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_LAYOUT:
					instance.LayoutConfiguration = serializer.Deserialize<LayoutConfiguration>(reader);
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}

	public sealed class LayoutConfigurationConverter : AbstractGenericJsonConverter<LayoutConfiguration>
	{
		private const string ATTR_SHARE_THUMB = "Sharethumb";
		private const string ATTR_STYLE = "Style";
		private const string ATTR_SIZE = "Size";
		private const string ATTR_POSITION = "Position";

		protected override void WriteProperties(JsonWriter writer, LayoutConfiguration value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.ShareThumb != null)
				writer.WriteProperty(ATTR_SHARE_THUMB, value.ShareThumb);

			if (value.Style != null)
				writer.WriteProperty(ATTR_STYLE, value.Style);

			if (value.Size != null)
				writer.WriteProperty(ATTR_SIZE, value.Size);

			if (value.Position != null)
				writer.WriteProperty(ATTR_POSITION, value.Position);
		}

		protected override void ReadProperty(string property, JsonReader reader, LayoutConfiguration instance,
		                                     JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_SHARE_THUMB:
					instance.ShareThumb = reader.GetValueAsBool();
					break;
				case ATTR_STYLE:
					instance.Style = reader.GetValueAsEnum<eZoomLayoutStyle>();
					break;
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

	public sealed class CallLayoutStatusResponseConverter : AbstractZoomRoomResponseConverter<CallLayoutStatusResponse>
	{
		private const string ATTR_LAYOUT = "Layout";

		protected override void WriteProperties(JsonWriter writer, CallLayoutStatusResponse value,
		                                        JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.LayoutStatus != null)
			{
				writer.WritePropertyName("Layout");
				serializer.Serialize(writer, value.LayoutStatus);
			}
		}

		protected override void ReadProperty(string property, JsonReader reader, CallLayoutStatusResponse instance,
		                                     JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_LAYOUT:
					instance.LayoutStatus = serializer.Deserialize<CallLayoutStatus>(reader);
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}

	public sealed class CallLayoutStatusConverter : AbstractGenericJsonConverter<CallLayoutStatus>
	{
		private const string ATTR_CAN_ADJUST_FLOATING_VIDEO = "can_Adjust_Floating_Video";
		private const string ATTR_CAN_SWITCH_FLOATING_SHARE_CONTENT = "can_Switch_Floating_Share_Content";
		private const string ATTR_CAN_SWITCH_SHARE_ON_ALL_SCREENS = "can_Switch_Share_On_All_Screens";
		private const string ATTR_CAN_SWITCH_SPEAKER_VIEW = "can_Switch_Speaker_View";
		private const string ATTR_CAN_SWITCH_WALL_VIEW = "can_Switch_Wall_View";
		private const string ATTR_IS_IN_FIRST_PAGE = "is_In_First_Page";
		private const string ATTR_IS_IN_LAST_PAGE = "is_In_Last_Page";
		private const string ATTR_IS_SUPPORTED = "is_supported";
		private const string ATTR_VIDEO_COUNT_IN_CURRENT_PAGE = "video_Count_In_Current_Page";
		private const string ATTR_VIDEO_TYPE = "video_type";

		protected override void WriteProperties(JsonWriter writer, CallLayoutStatus value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if(value.CanAdjustFloatingVideo)
				writer.WriteProperty(ATTR_CAN_ADJUST_FLOATING_VIDEO, value.CanAdjustFloatingVideo);

			if (value.CanSwitchFloatingShareContent)
				writer.WriteProperty(ATTR_CAN_SWITCH_FLOATING_SHARE_CONTENT, value.CanSwitchFloatingShareContent);

			if (value.CanSwitchShareOnAllScreens)
				writer.WriteProperty(ATTR_CAN_SWITCH_SHARE_ON_ALL_SCREENS, value.CanSwitchShareOnAllScreens);

			if (value.CanSwitchSpeakerView)
				writer.WriteProperty(ATTR_CAN_SWITCH_SPEAKER_VIEW, value.CanSwitchSpeakerView);

			if (value.CanSwitchWallView)
				writer.WriteProperty(ATTR_CAN_SWITCH_WALL_VIEW, value.CanSwitchWallView);

			if (value.IsInFirstPage)
				writer.WriteProperty(ATTR_IS_IN_FIRST_PAGE, value.IsInFirstPage);

			if (value.IsInLastPage)
				writer.WriteProperty(ATTR_IS_IN_LAST_PAGE, value.IsInLastPage);

			if (value.IsSupported)
				writer.WriteProperty(ATTR_IS_SUPPORTED, value.IsSupported);

			if (value.VideoCountInCurrentPage != 0)
				writer.WriteProperty(ATTR_VIDEO_COUNT_IN_CURRENT_PAGE, value.VideoCountInCurrentPage);

			if (value.VideoType != 0)
				writer.WriteProperty(ATTR_VIDEO_TYPE, value.VideoType);
		}

		protected override void ReadProperty(string property, JsonReader reader, CallLayoutStatus instance,
		                                     JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_CAN_ADJUST_FLOATING_VIDEO:
					instance.CanAdjustFloatingVideo = reader.GetValueAsBool();
					break;
				case ATTR_CAN_SWITCH_FLOATING_SHARE_CONTENT:
					instance.CanSwitchFloatingShareContent = reader.GetValueAsBool();
					break;
				case ATTR_CAN_SWITCH_SHARE_ON_ALL_SCREENS:
					instance.CanSwitchShareOnAllScreens = reader.GetValueAsBool();
					break;
				case ATTR_CAN_SWITCH_SPEAKER_VIEW:
					instance.CanSwitchSpeakerView = reader.GetValueAsBool();
					break;
				case ATTR_CAN_SWITCH_WALL_VIEW:
					instance.CanSwitchWallView = reader.GetValueAsBool();
					break;
				case ATTR_IS_IN_FIRST_PAGE:
					instance.IsInFirstPage = reader.GetValueAsBool();
					break;
				case ATTR_IS_IN_LAST_PAGE:
					instance.IsInLastPage = reader.GetValueAsBool();
					break;
				case ATTR_IS_SUPPORTED:
					instance.IsSupported = reader.GetValueAsBool();
					break;
				case ATTR_VIDEO_COUNT_IN_CURRENT_PAGE:
					instance.VideoCountInCurrentPage = reader.GetValueAsInt();
					break;
				case ATTR_VIDEO_TYPE:
					instance.VideoType = reader.GetValueAsEnum<eZoomLayoutVideoType>();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}
