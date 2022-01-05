#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using ICD.Connect.Conferencing.DialContexts;

namespace ICD.Connect.Conferencing.Contacts
{
	[JsonConverter(typeof(ContactJsonConverter))]
	public sealed class Contact : IContact
	{
		/// <summary>
		/// Gets/sets the contact name.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets/sets the dial contexts.
		/// </summary>
		public List<IDialContext> DialContexts { get; set; } 

		/// <summary>
		/// Gets the contact methods.
		/// </summary>
		/// <returns></returns>
		[NotNull]
		IEnumerable<IDialContext> IContact.GetDialContexts()
		{
			return DialContexts ?? Enumerable.Empty<IDialContext>();
		}
	}

	public sealed class ContactJsonConverter : AbstractGenericJsonConverter<Contact>
	{
		private const string TOKEN_NAME = "name";
		private const string TOKEN_DIAL_CONTEXTS = "dialContexts";

		/// <summary>
		/// Creates a new instance of T.
		/// </summary>
		/// <returns></returns>
		protected override Contact Instantiate()
		{
			return new Contact();
		}

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, Contact value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.Name != null)
				writer.WriteProperty(TOKEN_NAME, value.Name);

			DialContext[] dialContexts = value.DialContexts.Select(c => DialContext.Copy(c)).ToArray();
			if (dialContexts.Length > 0)
			{
				writer.WritePropertyName(TOKEN_DIAL_CONTEXTS);
				serializer.SerializeArray(writer, dialContexts);
			}
		}

		/// <summary>
		/// Override to handle the current property value with the given name.
		/// </summary>
		/// <param name="property"></param>
		/// <param name="reader"></param>
		/// <param name="instance"></param>
		/// <param name="serializer"></param>
		protected override void ReadProperty(string property, JsonReader reader, Contact instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case TOKEN_NAME:
					instance.Name = reader.GetValueAsString();
					break;

				case TOKEN_DIAL_CONTEXTS:
					instance.DialContexts = serializer.DeserializeArray<DialContext>(reader).Cast<IDialContext>().ToList();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}
