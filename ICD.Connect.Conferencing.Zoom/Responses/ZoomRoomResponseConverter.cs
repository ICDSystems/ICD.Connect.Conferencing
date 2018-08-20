using System;
using System.Linq;
#if SIMPLSHARP
using Crestron.SimplSharp.Reflection;
#else
using System.Reflection;
#endif
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	public sealed class ZoomRoomResponseConverter : AbstractGenericJsonConverter<AbstractZoomRoomResponse>
	{
#if SIMPLSHARP
		private static CType[] s_types;
		private static CType[] Types
#else
		private static Type[] s_types;
		private static Type[] Types
#endif
		{
			get { return s_types ?? (s_types = typeof (ZoomRoomResponseConverter).GetAssembly().GetTypes()); }
		}

		/// <summary>
		/// Key to the property in the json which stores where the actual response data is stored
		/// </summary>
		private const string RESPONSE_KEY = "topKey";

		/// <summary>
		/// Key to the property in the json that stores the type of response (zCommand, zConfiguration, zEvent, zStatus)
		/// </summary>
		private const string API_RESPONSE_TYPE = "type";

		/// <summary>
		/// Key to the property in the json that stores whether the response was synchronous to a command, or an async event
		/// </summary>
		private const string SYNCHRONOUS = "Sync";

		public override bool CanWrite { get { return false; } }

		public override void WriteJson(JsonWriter writer, AbstractZoomRoomResponse value, JsonSerializer serializer)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="existingValue"></param>
		/// <param name="serializer"></param>
		/// <returns></returns>
		public override AbstractZoomRoomResponse ReadJson(JsonReader reader, AbstractZoomRoomResponse existingValue, JsonSerializer serializer)
		{
			JObject jObject = JObject.Load(reader);
			string responseKey = jObject[RESPONSE_KEY].ToString();
			eZoomRoomApiType apiResponseType = jObject[API_RESPONSE_TYPE].ToObject<eZoomRoomApiType>();
			bool synchronous = jObject[SYNCHRONOUS].ToObject<bool>();

			// find concrete type that matches the json values
			var responseType = Types.SingleOrDefault(t => TypeAttributeMatchesParams(t, responseKey, apiResponseType, synchronous));
			if(responseType != null)
				return (AbstractZoomRoomResponse)serializer.Deserialize(new JTokenReader(jObject), responseType);
			return null;
		}

		/// <summary>
		/// Checks if the given type has a ZoomRoomApiResponse attribute with the given values
		/// </summary>
		/// <param name="type"></param>
		/// <param name="key"></param>
		/// <param name="responseType"></param>
		/// <param name="synchronous"></param>
		/// <returns></returns>
#if SIMPLSHARP
		private bool TypeAttributeMatchesParams(CType type, string key, eZoomRoomApiType responseType, bool synchronous)
#else
		private bool TypeAttributeMatchesParams(Type type, string key, eZoomRoomApiType responseType, bool synchronous)
#endif
		{
			var attributes = type.GetCustomAttributes<ZoomRoomApiResponseAttribute>();
			return attributes.Any(a =>
				a.ResponseKey == key &&
				a.CommandType == responseType &&
				a.Synchronous == synchronous);
		}
	}
}