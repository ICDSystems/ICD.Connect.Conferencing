using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Xml;
using ICD.Connect.Conferencing.Contacts;
using ICD.Connect.Conferencing.DialingPlans.Matchers;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.DialingPlans
{
	/// <summary>
	/// XmlDialingPlan loads a configuration from XML.
	/// </summary>
	public sealed class DialingPlan
	{
		private readonly Dictionary<IPlanMatcher, int> m_Matchers;
		private readonly SafeCriticalSection m_MatchersSection;

		private eConferenceSourceType m_DefaultSourceType;

		#region Properties

		/// <summary>
		/// The default source type.
		/// </summary>
		public eConferenceSourceType DefaultSourceType { get { return m_DefaultSourceType; } }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		public DialingPlan()
		{
			m_Matchers = new Dictionary<IPlanMatcher, int>();
			m_MatchersSection = new SafeCriticalSection();

			m_DefaultSourceType = eConferenceSourceType.Audio;
		}

		#endregion

		#region Matchers

		/// <summary>
		/// Clears the collection of matchers.
		/// </summary>
		public void ClearMatchers()
		{
			m_MatchersSection.Execute(() => m_Matchers.Clear());
		}

		/// <summary>
		/// Parses the xml to build the collection of matchers.
		/// </summary>
		/// <param name="xml"></param>
		public void LoadMatchersFromXml(string xml)
		{
			m_MatchersSection.Enter();

			try
			{
				ClearMatchers();

				string matchingXml = XmlUtils.GetChildElementAsString(xml, "Matching");
				string defaultString = XmlUtils.GetAttributeAsString(matchingXml, "default");

				m_DefaultSourceType = EnumUtils.Parse<eConferenceSourceType>(defaultString, true);

				foreach (IcdXmlReader child in XmlUtils.GetChildElements(matchingXml))
				{
					ParseMatcher(child);
					child.Dispose();
				}
			}
			finally
			{
				m_MatchersSection.Leave();
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Gets the source type for the given number.
		/// </summary>
		/// <param name="number"></param>
		/// <returns></returns>
		public eConferenceSourceType GetSourceType(string number)
		{
			if (string.IsNullOrEmpty(number))
				return eConferenceSourceType.Unknown;

			IPlanMatcher matcher = GetMatcher(number);
			return matcher == null ? eConferenceSourceType.Unknown : matcher.SourceType;
		}

		/// <summary>
		/// Formats the phone number to a human readable format.
		/// </summary>
		/// <param name="number"></param>
		/// <returns></returns>
		public string FormatNumber(string number)
		{
			IPlanMatcher matcher = GetMatcher(number);
			return matcher != null ? matcher.FormatNumber(number) : number;
		}

		/// <summary>
		/// Gets the source type for the given contact.
		/// </summary>
		/// <param name="contact"></param>
		/// <returns></returns>
		public eConferenceSourceType GetSourceType(IContact contact)
		{
			if (contact == null)
				throw new ArgumentNullException("contact");

			IContactMethod contactMethod = GetContactMethod(contact);
			return contactMethod == null ? eConferenceSourceType.Unknown : GetSourceType(contactMethod);
		}

		/// <summary>
		/// Gets the source type for the given contact method.
		/// </summary>
		/// <param name="contactMethod"></param>
		/// <returns></returns>
		public eConferenceSourceType GetSourceType(IContactMethod contactMethod)
		{
			if (contactMethod == null)
				throw new ArgumentNullException("contactMethod");

			return GetSourceType(contactMethod.Number);
		}

		/// <summary>
		/// Gets the ideal contact method for the given contact.
		/// </summary>
		/// <param name="contact"></param>
		/// <returns></returns>
		[CanBeNull]
		public IContactMethod GetContactMethod(IContact contact)
		{
			if (contact == null)
				throw new ArgumentNullException("contact");

			return GetContactMethod(contact, DefaultSourceType);
		}

		/// <summary>
		/// Gets the ideal contact method for the given contact and mode.
		/// </summary>
		/// <param name="contact"></param>
		/// <param name="mode"></param>
		/// <returns></returns>
		[CanBeNull]
		public IContactMethod GetContactMethod(IContact contact, eConferenceSourceType mode)
		{
			if (contact == null)
				throw new ArgumentNullException("contact");

			IContactMethod[] contactMethods = contact.GetContactMethods().ToArray();
			return contactMethods.FirstOrDefault(m => GetSourceType(m) == mode) ?? contactMethods.FirstOrDefault();
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Gets the first matcher that matches the given number.
		/// </summary>
		/// <param name="number"></param>
		/// <returns></returns>
		private IPlanMatcher GetMatcher(string number)
		{
			return m_Matchers.OrderBy(p => p.Value)
			                 .FirstOrDefault(p => p.Key.Matches(number)).Key;
		}

		/// <summary>
		/// Parses the xml for a single matcher.
		/// </summary>
		/// <param name="reader"></param>
		private void ParseMatcher(IcdXmlReader reader)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");

			IPlanMatcher matcher;

			eConferenceSourceType sourceType = EnumUtils.Parse<eConferenceSourceType>(reader.Name, true);
			string name = reader.GetAttribute("name");
			int order = reader.GetAttributeAsInt("order");

			string mode = null;
			string regex = null;
			string number = null;
			int minLength = 0;
			int maxLength = 0;
			string format = null;
			eFormatMode formatMode = eFormatMode.Normal;
			List<char> exclude = new List<char>();

			foreach (IcdXmlReader child in reader.GetChildElements())
			{
				switch (child.Name)
				{
					case "Mode":
						mode = child.ReadElementContentAsString();
						break;

					case "Regex":
						regex = child.ReadElementContentAsString();
						break;

					case "MinLength":
						minLength = child.ReadElementContentAsInt();
						break;

					case "MaxLength":
						maxLength = child.ReadElementContentAsInt();
						break;

					case "Exclude":
						exclude.AddRange(child.ReadElementContentAsString());
						break;

					case "Number":
						number = child.ReadElementContentAsString();
						break;

					case "DisplayFormat":
						string formatModeString = child.GetAttribute("mode");
						formatMode = string.IsNullOrEmpty(formatModeString)
							             ? eFormatMode.Normal
							             : EnumUtils.Parse<eFormatMode>(formatModeString, true);
						format = child.ReadElementContentAsString();
						break;

					default:
						throw new ArgumentOutOfRangeException("Unknown element: " + child.Name);
				}

				child.Dispose();
			}

			switch (mode)
			{
				case "Numeric":
					matcher = new NumericPlanMatcher(name, sourceType, format, formatMode, minLength, maxLength, exclude.ToArray());
					break;

				case "Regex":
					matcher = new RegexPlanMatcher(name, sourceType, format, formatMode, regex);
					break;

				case "Exact":
					matcher = new ExactPlanMatcher(name, sourceType, format, formatMode, number);
					break;

				default:
					throw new ArgumentOutOfRangeException("Unknown mode: " + mode);
			}

			m_Matchers[matcher] = order;
		}

		#endregion
	}
}
