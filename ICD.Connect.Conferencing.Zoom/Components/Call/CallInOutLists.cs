using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Components.Call
{
	[JsonConverter(typeof(CallInOutListsConverter))]
	public sealed class CallInOutLists
	{
		public List<CallInOutListEntry> CalloutCountryList { get; set; }
		public List<CallInOutListEntry> CallinCountryList { get; set; }
		public List<CallInOutListEntry> TollFreeCallinList { get; set; }
	}

	public sealed class CallInOutListsConverter : AbstractGenericJsonConverter<CallInOutLists>
	{
		private const string ATTR_CALLOUT_COUNTRY_LIST = "callout_country_list";
		private const string ATTR_CALLIN_COUNTRY_LIST = "callin_country_list";
		private const string ATTR_TOLL_FREE_CALLIN_LIST = "toll_free_callin_list";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, CallInOutLists value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.CalloutCountryList != null)
			{
				writer.WritePropertyName(ATTR_CALLOUT_COUNTRY_LIST);
				serializer.SerializeArray(writer, value.CalloutCountryList);
			}

			if (value.CallinCountryList != null)
			{
				writer.WritePropertyName(ATTR_CALLIN_COUNTRY_LIST);
				serializer.SerializeArray(writer, value.CallinCountryList);
			}

			if (value.TollFreeCallinList != null)
			{
				writer.WritePropertyName(ATTR_TOLL_FREE_CALLIN_LIST);
				serializer.SerializeArray(writer, value.TollFreeCallinList);
			}
		}

		/// <summary>
		/// Override to handle the current property value with the given name.
		/// </summary>
		/// <param name="property"></param>
		/// <param name="reader"></param>
		/// <param name="instance"></param>
		/// <param name="serializer"></param>
		protected override void ReadProperty(string property, JsonReader reader, CallInOutLists instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_CALLOUT_COUNTRY_LIST:
					instance.CalloutCountryList = serializer.DeserializeArray<CallInOutListEntry>(reader).ToList();
					break;

				case ATTR_CALLIN_COUNTRY_LIST:
					instance.CallinCountryList = serializer.DeserializeArray<CallInOutListEntry>(reader).ToList();
					break;

				case ATTR_TOLL_FREE_CALLIN_LIST:
					instance.TollFreeCallinList = serializer.DeserializeArray<CallInOutListEntry>(reader).ToList();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}