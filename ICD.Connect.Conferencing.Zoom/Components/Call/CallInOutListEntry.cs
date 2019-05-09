using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Components.Call
{
	[JsonConverter(typeof(CallInOutListEntryConverter))]
	public sealed class CallInOutListEntry
	{
		public string Id { get; set; }
		public string Name { get; set; }
		public string Code { get; set; }
		public string Number { get; set; }
		public string DisplayNumber { get; set; }
	}

	public sealed class CallInOutListEntryConverter : AbstractGenericJsonConverter<CallInOutListEntry>
	{
		private const string ATTR_ID = "id";
		private const string ATTR_NAME = "name";
		private const string ATTR_CODE = "code";
		private const string ATTR_NUMBER = "number";
		private const string ATTR_DISPLAY_NUMBER = "display_number";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, CallInOutListEntry value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.Id != null)
				writer.WriteProperty(ATTR_ID, value.Id);

			if (value.Name != null)
				writer.WriteProperty(ATTR_NAME, value.Name);

			if (value.Code != null)
				writer.WriteProperty(ATTR_CODE, value.Code);

			if (value.Number != null)
				writer.WriteProperty(ATTR_NUMBER, value.Number);

			if (value.DisplayNumber != null)
				writer.WriteProperty(ATTR_DISPLAY_NUMBER, value.DisplayNumber);
		}

		/// <summary>
		/// Override to handle the current property value with the given name.
		/// </summary>
		/// <param name="property"></param>
		/// <param name="reader"></param>
		/// <param name="instance"></param>
		/// <param name="serializer"></param>
		protected override void ReadProperty(string property, JsonReader reader, CallInOutListEntry instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_ID:
					instance.Id = reader.GetValueAsString();
					break;

				case ATTR_NAME:
					instance.Name = reader.GetValueAsString();
					break;

				case ATTR_CODE:
					instance.Code = reader.GetValueAsString();
					break;

				case ATTR_NUMBER:
					instance.Number = reader.GetValueAsString();
					break;

				case ATTR_DISPLAY_NUMBER:
					instance.DisplayNumber = reader.GetValueAsString();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}