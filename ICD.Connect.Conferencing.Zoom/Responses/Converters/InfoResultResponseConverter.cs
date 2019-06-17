﻿using ICD.Common.Utils.Json;
using ICD.Connect.Conferencing.Zoom.Components.Call;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses.Converters
{
	public sealed class InfoResultResponseConverter : AbstractGenericJsonConverter<InfoResultResponse>
	{
		private const string ATTR_INFO_RESULT = "InfoResult";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, InfoResultResponse value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.InfoResult != null)
			{
				writer.WritePropertyName(ATTR_INFO_RESULT);
				serializer.Serialize(writer, value.InfoResult);
			}
		}

		protected override void ReadProperty(string property, JsonReader reader, InfoResultResponse instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_INFO_RESULT:
					instance.InfoResult = serializer.Deserialize<CallInfo>(reader);
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}