using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using ICD.Connect.Conferencing.Zoom.Components.Presentation;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses.Converters
{
	public sealed class SharingStateInfoConverter : AbstractGenericJsonConverter<SharingStateInfo>
	{
		private const string ATTR_PAUSED = "paused";
		private const string ATTR_STATE = "state";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, SharingStateInfo value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.Paused)
				writer.WriteProperty(ATTR_PAUSED, value.Paused);

			if (value.State != default(eSharingState))
				writer.WriteProperty(ATTR_STATE, value.State);
		}

		protected override void ReadProperty(string property, JsonReader reader, SharingStateInfo instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_PAUSED:
					instance.Paused = reader.GetValueAsBool();
					break;

				case ATTR_STATE:
					instance.State = reader.GetValueAsEnum<eSharingState>();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}
