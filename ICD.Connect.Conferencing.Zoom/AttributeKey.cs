using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ICD.Common.Properties;
#if SIMPLSHARP
using Crestron.SimplSharp.Reflection;
#else
using System.Reflection;
#endif
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Zoom.Responses;
using ICD.Connect.Conferencing.Zoom.Responses.Attributes;

namespace ICD.Connect.Conferencing.Zoom
{
	public sealed class AttributeKey : IEquatable<AttributeKey>
	{
		private const string ATTR_KEY_REGEX =
			@"""Sync"": (?'Sync'true|false),\s*""topKey"": ""(?'topKey'.*)"",\s*""type"": ""(?'type'zConfiguration|zEvent|zStatus|zCommand)""";

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

		private readonly string m_Key;
		private readonly eZoomRoomApiType m_ResponseType;
		private readonly bool m_Synchronous;

		#region Properties

		public string Key { get { return m_Key; } }

		public eZoomRoomApiType ResponseType { get { return m_ResponseType; } }

		public bool Synchronous { get { return m_Synchronous; } }

		#endregion

		#region Constructors

		/// <summary>
		/// Static constructor.
		/// </summary>
		static AttributeKey()
		{
			s_TypeDict = new Dictionary<AttributeKey, Type>();

			foreach (
#if SIMPLSHARP
				CType
#else
				Type
#endif
					type in typeof(ZoomRoom).GetAssembly().GetTypes())
			{
				foreach (ZoomRoomApiResponseAttribute attribute in type.GetCustomAttributes<ZoomRoomApiResponseAttribute>())
				{
					AttributeKey key = new AttributeKey(attribute);
					s_TypeDict.Add(key, type);
				}
			}
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="type"></param>
		/// <param name="synchronous"></param>
		public AttributeKey(string key, eZoomRoomApiType type, bool synchronous)
		{
			m_Key = key;
			m_ResponseType = type;
			m_Synchronous = synchronous;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="attribute"></param>
		public AttributeKey(ZoomRoomApiResponseAttribute attribute)
			: this(attribute.ResponseKey, attribute.CommandType, attribute.Synchronous)
		{
		}

		#endregion

		#region Methods

		public override string ToString()
		{
			return string.Format("\"Sync\": {0}, \"topKey\": \"{1}\", \"type\": \"{2}\"",
			                     m_Synchronous.ToString().ToLower(), m_Key, m_ResponseType);
		}

		public static bool TryParse(string data, out AttributeKey output)
		{
			output = null;

			// Avoid regexing through thousands of lines of JSON
			int start = data.LastIndexOf("\"Sync\"", StringComparison.Ordinal);
			if (start < 0)
				return false;

			data = data.Substring(start);

			Match match;
			if (!RegexUtils.Matches(data, ATTR_KEY_REGEX, out match))
				return false;

			eZoomRoomApiType apiResponseType;
			if (!EnumUtils.TryParse(match.Groups[API_RESPONSE_TYPE].Value, true, out apiResponseType))
				return false;

			string responseKey = match.Groups[RESPONSE_KEY].Value;
			bool synchronous = bool.Parse(match.Groups[SYNCHRONOUS].Value);

			output = new AttributeKey(responseKey, apiResponseType, synchronous);
			return true;
		}

		[CanBeNull]
		public Type GetResponseType()
		{
			return s_TypeDict.GetDefault(this);
		}

		#endregion

		#region Equality

		public bool Equals(AttributeKey other)
		{
			if (ReferenceEquals(null, other))
				return false;

			if (ReferenceEquals(this, other))
				return true;

			return String.Equals(m_Key, other.m_Key) &&
			       m_ResponseType == other.m_ResponseType &&
			       m_Synchronous == other.m_Synchronous;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;

			if (ReferenceEquals(this, obj))
				return true;

			return obj is AttributeKey && Equals((AttributeKey)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = (m_Key != null ? m_Key.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (int)m_ResponseType;
				hashCode = (hashCode * 397) ^ m_Synchronous.GetHashCode();
				return hashCode;
			}
		}

		#endregion
	}
}
