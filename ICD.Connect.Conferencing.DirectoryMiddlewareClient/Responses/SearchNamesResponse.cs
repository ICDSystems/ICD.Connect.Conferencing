#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;

namespace ICD.Connect.Conferencing.DirectoryMiddlewareClient.Responses
{
	[JsonConverter(typeof(SearchNamesResponseJsonConverter))]
	public sealed class SearchNamesResponse
	{
		public int Page { get; set; }
		public int TotalCount { get; set; }
		public List<ContactName> Results { get; set; }
	}

	[JsonConverter(typeof(ContactNameJsonConverter))]
	public sealed class ContactName
	{
		public Guid Id { get; set; }
		public string Name { get; set; }
	}

	public class SearchNamesResponseJsonConverter : AbstractGenericJsonConverter<SearchNamesResponse>
	{
		private const string TOKEN_PAGE = "page";
		private const string TOKEN_TOTAL_COUNT = "totalCount";
		private const string TOKEN_RESULTS = "results";

		/// <summary>
		/// Creates a new instance of T.
		/// </summary>
		/// <returns></returns>
		protected override SearchNamesResponse Instantiate()
		{
			return new SearchNamesResponse();
		}

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, SearchNamesResponse value, JsonSerializer serializer)
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
		protected override void ReadProperty(string property, JsonReader reader, SearchNamesResponse instance, JsonSerializer serializer)
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
					instance.Results = serializer.DeserializeArray<ContactName>(reader).ToList();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}

	public class ContactNameJsonConverter : AbstractGenericJsonConverter<ContactName>
	{
		private const string TOKEN_ID = "id";
		private const string TOKEN_NAME = "name";

		protected override ContactName Instantiate()
		{
			return new ContactName();
		}

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, ContactName value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.Id != Guid.Empty)
				writer.WriteProperty(TOKEN_ID, value.Id);

			if (value.Name != null)
				writer.WriteProperty(TOKEN_NAME, value.Name);
		}

		/// <summary>
		/// Override to handle the current property value with the given name.
		/// </summary>
		/// <param name="property"></param>
		/// <param name="reader"></param>
		/// <param name="instance"></param>
		/// <param name="serializer"></param>
		protected override void ReadProperty(string property, JsonReader reader, ContactName instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case TOKEN_ID:
					instance.Id = reader.GetValueAsGuid();
					break;

				case TOKEN_NAME:
					instance.Name = reader.GetValueAsString();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}
