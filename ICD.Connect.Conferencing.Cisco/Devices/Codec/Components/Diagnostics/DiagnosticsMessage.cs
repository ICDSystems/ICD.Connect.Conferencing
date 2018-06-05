using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Xml;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Diagnostics
{
	/// <summary>
	/// DiagnosticsMessage represents feedback from the codec diagnostics status.
	/// </summary>
	public struct DiagnosticsMessage
	{
		/// <summary>
		/// Message severity level.
		/// </summary>
		public enum eLevel
		{
#pragma warning disable 1591
			Ok,
			Warning,
			Error,
			Critical
#pragma warning restore 1591
		}

		private readonly string m_Description;
		private readonly eLevel m_Level;
		private readonly string m_References;
		private readonly string m_Type;

		#region Properties

		/// <summary>
		/// The text body of the message.
		/// </summary>
		public string Description { get { return m_Description; } }

		/// <summary>
		/// Message severity level.
		/// </summary>
		public eLevel Level { get { return m_Level; } }

		/// <summary>
		/// Related message data.
		/// </summary>
		public string References { get { return m_References; } }

		/// <summary>
		/// The type of message.
		/// </summary>
		public string Type { get { return m_Type; } }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="description"></param>
		/// <param name="level"></param>
		/// <param name="references"></param>
		/// <param name="type"></param>
		[PublicAPI]
		public DiagnosticsMessage(string description, eLevel level, string references, string type)
		{
			m_Description = description;
			m_Level = level;
			m_References = references;
			m_Type = type;
		}

		/// <summary>
		/// Instantiates a new DiagnosticsMessage from an xml element.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		public static DiagnosticsMessage FromXml(string xml)
		{
			string description = XmlUtils.TryReadChildElementContentAsString(xml, "Description");
			string references = XmlUtils.TryReadChildElementContentAsString(xml, "References");
			string type = XmlUtils.TryReadChildElementContentAsString(xml, "Type");

			eLevel level;
			if (!XmlUtils.TryReadChildElementContentAsEnum(xml, "Level", true, out level))
				level = eLevel.Ok;

			bool ghost;
			if (TryUtils.Try(XmlUtils.GetAttributeAsBool, xml, "ghost", out ghost) && ghost)
				level = eLevel.Ok;

			return new DiagnosticsMessage(description, level, references, type);
		}

		/// <summary>
		/// Returns the message with level set to Ok.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		public static DiagnosticsMessage GetResolved(DiagnosticsMessage message)
		{
			return new DiagnosticsMessage(message.Description, eLevel.Ok, message.References, message.Type);
		}

		#endregion

		#region Methods

		/// <summary>
		/// Gets the string representation of the instance.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return string.Format("{0}: {1} - {2} ({3})", Level, Type, Description, References);
		}

		/// <summary>
		/// Returns true if the issues are the same, regardless of level.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool IsSameIssue(DiagnosticsMessage other)
		{
			return Type == other.Type && Description == other.Description;
		}

		/// <summary>
		/// Implementing default equality.
		/// </summary>
		/// <param name="m1"></param>
		/// <param name="m2"></param>
		/// <returns></returns>
		public static bool operator ==(DiagnosticsMessage m1, DiagnosticsMessage m2)
		{
			return m1.Equals(m2);
		}

		/// <summary>
		/// Implementing default inequality.
		/// </summary>
		/// <param name="m1"></param>
		/// <param name="m2"></param>
		/// <returns></returns>
		public static bool operator !=(DiagnosticsMessage m1, DiagnosticsMessage m2)
		{
			return !(m1 == m2);
		}

		/// <summary>
		/// Returns true if this instance is equal to the given object.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override bool Equals(object other)
		{
			if (other == null || GetType() != other.GetType())
				return false;

			return GetHashCode() == ((DiagnosticsMessage)other).GetHashCode();
		}

		/// <summary>
		/// Gets the hashcode for this instance.
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = hash * 23 + (m_Description == null ? 0 : m_Description.GetHashCode());
				hash = hash * 23 + (int)m_Level;
				hash = hash * 23 + (m_References == null ? 0 : m_References.GetHashCode());
				hash = hash * 23 + (m_Type == null ? 0 : m_Type.GetHashCode());
				return hash;
			}
		}

		#endregion
	}
}
