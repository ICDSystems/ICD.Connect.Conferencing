using System;
using System.Collections.Generic;
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

		private static readonly Dictionary<AttributeKey, Type> s_TypeDict;

		public override bool CanWrite { get { return false; } }

		/// <summary>
		/// Static constructor.
		/// </summary>
		static ZoomRoomResponseConverter()
		{
			s_TypeDict = new Dictionary<AttributeKey, Type>();

			foreach (
#if SIMPLSHARP
				CType
#else
				Type
#endif
					type in typeof(ZoomRoomResponseConverter).GetAssembly().GetTypes())
			{
				foreach (ZoomRoomApiResponseAttribute attribute in type.GetCustomAttributes<ZoomRoomApiResponseAttribute>())
				{
					AttributeKey key = new AttributeKey(attribute);
					s_TypeDict.Add(key, type);
				}
			}
		}

		protected override AbstractZoomRoomResponse Instantiate()
		{
			throw new NotImplementedException();
		}

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
		    try
		    {
		        JObject jObject = JObject.Load(reader);
		        string responseKey = jObject[RESPONSE_KEY].ToString();
		        eZoomRoomApiType apiResponseType = jObject[API_RESPONSE_TYPE].ToObject<eZoomRoomApiType>();
		        bool synchronous = jObject[SYNCHRONOUS].ToObject<bool>();

		        AttributeKey key = new AttributeKey(responseKey, apiResponseType, synchronous);

		        // find concrete type that matches the json values
		        Type responseType;
		        if (!s_TypeDict.TryGetValue(key, out responseType))
		            return null;

		        // shitty zoom api sometimes sends a single object instead of array
		        if (responseType == typeof(ListParticipantsResponse) && jObject[responseKey].Type != JTokenType.Array)
		            responseType = typeof(SingleParticipantResponse);

		        if (responseType != null)
		            return (AbstractZoomRoomResponse) serializer.Deserialize(new JTokenReader(jObject), responseType);
                
		        return null;
		    }
		    catch(JsonException ex)
		    {
		        return null;
		    }
		}

		private sealed class AttributeKey : IEquatable<AttributeKey>
		{
			private readonly string m_Key;
			private readonly eZoomRoomApiType m_ResponseType;
			private readonly bool m_Synchronous;

			public string Key
			{
				get { return m_Key; }
			}
			public eZoomRoomApiType ResponseType
			{
				get { return m_ResponseType; }
			}
			public bool Synchronous
			{
				get { return m_Synchronous; }
			}

			public AttributeKey(string key, eZoomRoomApiType type, bool synchronous)
			{
				m_Key = key;
				m_ResponseType = type;
				m_Synchronous = synchronous;
			}

			public AttributeKey(ZoomRoomApiResponseAttribute attribute)
				: this(attribute.ResponseKey, attribute.CommandType, attribute.Synchronous)
			{
			}

			public bool Equals(AttributeKey other)
			{
				if (ReferenceEquals(null, other)) return false;
				if (ReferenceEquals(this, other)) return true;
				return string.Equals(m_Key, other.m_Key) && m_ResponseType == other.m_ResponseType && m_Synchronous == other.m_Synchronous;
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				return obj is AttributeKey && Equals((AttributeKey) obj);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					var hashCode = (m_Key != null ? m_Key.GetHashCode() : 0);
					hashCode = (hashCode * 397) ^ (int) m_ResponseType;
					hashCode = (hashCode * 397) ^ m_Synchronous.GetHashCode();
					return hashCode;
				}
			}
		}
	}
}
