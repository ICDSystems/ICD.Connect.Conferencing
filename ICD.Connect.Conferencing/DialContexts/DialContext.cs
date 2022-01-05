#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using System;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.DialContexts
{
	[JsonConverter(typeof(DialContextJsonConverter))]
	public sealed class DialContext : AbstractDialContext
	{
		/// <summary>
		/// Creates a copy of the given dial context.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		[NotNull]
		public static DialContext Copy([NotNull] IDialContext other)
		{
			if (other == null)
				throw new ArgumentNullException("other");

			return new DialContext
			{
				Protocol = other.Protocol,
				CallType = other.CallType,
				DialString = other.DialString,
				Password = other.Password
			};
		}
	}

	public sealed class DialContextJsonConverter : AbstractGenericJsonConverter<DialContext>
	{
		private const string TOKEN_PROTOCOL = "protocol";
		private const string TOKEN_CALL_TYPE = "callType";
		private const string TOKEN_DIAL_STRING = "dialString";
		private const string TOKEN_PASSWORD = "password";

		/// <summary>
		/// Creates a new instance of T.
		/// </summary>
		/// <returns></returns>
		protected override DialContext Instantiate()
		{
			return new DialContext();
		}

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, DialContext value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.Protocol != default(eDialProtocol))
				writer.WriteProperty(TOKEN_PROTOCOL, value.Protocol.ToString());

			if (value.CallType != default(eCallType))
				writer.WriteProperty(TOKEN_CALL_TYPE, value.CallType);

			if (value.DialString != null)
				writer.WriteProperty(TOKEN_DIAL_STRING, value.DialString);

			if (value.Password != null)
				writer.WriteProperty(TOKEN_PASSWORD, value.Password);
		}

		/// <summary>
		/// Override to handle the current property value with the given name.
		/// </summary>
		/// <param name="property"></param>
		/// <param name="reader"></param>
		/// <param name="instance"></param>
		/// <param name="serializer"></param>
		protected override void ReadProperty(string property, JsonReader reader, DialContext instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case TOKEN_PROTOCOL:
					instance.Protocol = reader.GetValueAsEnum<eDialProtocol>();
					break;

				case TOKEN_CALL_TYPE:
					instance.CallType = reader.GetValueAsEnum<eCallType>();
					break;

				case TOKEN_DIAL_STRING:
					instance.DialString = reader.GetValueAsString();
					break;

				case TOKEN_PASSWORD:
					instance.Password = reader.GetValueAsString();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}
