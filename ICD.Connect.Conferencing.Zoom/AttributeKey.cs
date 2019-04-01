using System;
using System.Text.RegularExpressions;
using ICD.Common.Utils;
using ICD.Connect.Conferencing.Zoom.Responses;
using ICD.Connect.Conferencing.Zoom.Responses.Attributes;

namespace ICD.Connect.Conferencing.Zoom
{
	public sealed class AttributeKey : IEquatable<AttributeKey>
	{
		private const string ATTR_KEY_REGEX =
			"\"Sync\": (?'sync'true|false),\r\n  \"topKey\": \"(?'topKey'.*)\",\r\n  \"type\": \"(?'type'zConfiguration|zEvent|zStatus|zCommand)\"";

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
		private const string SYNCHRONOUS = "sync";

		private readonly string m_Key;
		private readonly eZoomRoomApiType m_ResponseType;
		private readonly bool m_Synchronous;

		public string Key { get { return m_Key; } }

		public eZoomRoomApiType ResponseType { get { return m_ResponseType; } }

		public bool Synchronous { get { return m_Synchronous; } }

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

		public static bool TryParse(string data, out AttributeKey output)
		{
			output = null;

			Match match;
			if (!RegexUtils.Matches(data, ATTR_KEY_REGEX, out match))
				return false;

			eZoomRoomApiType apiResponseType;
			if (!EnumUtils.TryParse(match.Groups[API_RESPONSE_TYPE].Value, true, out apiResponseType))
				return false;

			string responseKey = match.Groups[RESPONSE_KEY].Value;

			IcdConsole.PrintLine(eConsoleColor.Magenta, "Sync: {0}", match.Groups[SYNCHRONOUS].Value);

			bool synchronous = bool.Parse(match.Groups[SYNCHRONOUS].Value);

			output = new AttributeKey(responseKey, apiResponseType, synchronous);
			return true;
		}

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
	}
}
