#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using ICD.Connect.Conferencing.Contacts;

namespace ICD.Connect.Conferencing.DirectoryMiddlewareClient.Responses
{
	[JsonConverter(typeof(SearchResponseJsonConverter))]
	public sealed class SearchResponse
	{
		public int Page { get; set; }
		public int TotalCount { get; set; }
		public List<Contact> Results { get; set; }
	}

	public class SearchResponseJsonConverter : AbstractGenericJsonConverter<SearchResponse>
	{
		private const string TOKEN_PAGE = "page";
		private const string TOKEN_TOTAL_COUNT = "totalCount";
		private const string TOKEN_RESULTS = "results";

		/// <summary>
		/// Creates a new instance of T.
		/// </summary>
		/// <returns></returns>
		protected override SearchResponse Instantiate()
		{
			return new SearchResponse();
		}

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, SearchResponse value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.Page != 0)
				writer.WriteProperty(TOKEN_PAGE, value.Page);

			if (value.TotalCount != 0)
				writer.WriteProperty(TOKEN_TOTAL_COUNT, value.TotalCount);

			if (value.Results.Count > 0)
			{
				writer.WritePropertyName(TOKEN_RESULTS);
				serializer.SerializeArray(writer, value.Results);
			}
		}

		/// <summary>
		/// Override to handle the current property value with the given name.
		/// </summary>
		/// <param name="property"></param>
		/// <param name="reader"></param>
		/// <param name="instance"></param>
		/// <param name="serializer"></param>
		protected override void ReadProperty(string property, JsonReader reader, SearchResponse instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case TOKEN_PAGE:
					instance.Page = reader.GetValueAsInt();
					break;

				case TOKEN_TOTAL_COUNT:
					instance.TotalCount = reader.GetValueAsInt();
					break;

				case TOKEN_RESULTS:
					instance.Results = serializer.DeserializeArray<Contact>(reader).ToList();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}
