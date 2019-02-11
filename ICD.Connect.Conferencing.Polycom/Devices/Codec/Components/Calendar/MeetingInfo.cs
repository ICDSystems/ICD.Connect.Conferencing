using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Dial;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Calendar
{
	public sealed class MeetingInfo
	{
		// id|AAAlAGNvbmZyb29tQHByb2ZvdW5kdGVjaC5vbm1pY3Jvc29mdC5jb20BUQAICNYOC4d8QABGAAAAAKDEA4kOv71Is4nKeHnLHVUHAKtBwbBNzutGjFgfJdA+tJAAAAAAAQ0AAKtBwbBNzutGjFgfJdA+tJAAAFcf13UAABA=
		private const string ID_REGEX = @"id\|(?'id'[^|]*)";
		
		// 2018-08-30:12:30|2018-08-30:14:30|dialable|public
		private const string DATES_REGEX = @"(?'start'[^|]*)\|(?'end'[^|]*)\|(?'dialable'dialable|nondialable)\|(?'public'public|private)";

		// organizer|Chris VanLuvanee
		private const string ORGANIZER_REGEX = @"organizer\|(?'organizer'[^|]*)";

		// location|Online meeting; ConfRoom
		private const string LOCATION_REGEX = @"location\|(?'location'[^|]*)";

		// subject|Chris VanLuvanee
		private const string SUBJECT_REGEX = @"subject\|(?'subject'[^|]*)";

		// meetingpassword|none
		private const string MEETING_PASSWORD_REGEX = @"meetingpassword\|(?'meetingpassword'[^|]*)";

		// attendee|Chris VanLuvanee
		private const string ATTENDEE_REGEX = @"attendee\|(?'attendee'[^|]*)";

		private readonly List<DialingNumber> m_DialingNumbers;
		private readonly List<string> m_Attendees;

		public string Id { get; private set; }
		public DateTime Start { get; private set; }
		public DateTime End { get; private set; }
		public bool Dialable { get; private set; }
		public bool Public { get; private set; }
		public string Organizer { get; private set; }
		public string Location { get; private set; }
		public string Subject { get; private set; }
		public string MeetingPassword { get; private set; }

		/// <summary>
		/// Constructor.
		/// </summary>
		private MeetingInfo()
		{
			m_DialingNumbers = new List<DialingNumber>();
			m_Attendees = new List<string>();
		}

		/// <summary>
		/// Gets the dialing numbers for the meeting info.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<DialingNumber> GetDialingNumbers()
		{
			return m_DialingNumbers.ToArray(m_DialingNumbers.Count);
		}

		/// <summary>
		/// Gets the attendees for the meeting info.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<string> GetAttendees()
		{
			return m_Attendees.ToArray(m_Attendees.Count);
		}

		/// <summary>
		/// Parses the meeting info strings and returns a MeetingInfo instance.
		/// </summary>
		/// <param name="lines"></param>
		/// <returns></returns>
		public static MeetingInfo FromString(IEnumerable<string> lines)
		{
			if (lines == null)
				throw new ArgumentNullException("lines");

			MeetingInfo output = new MeetingInfo();

			foreach (string line in lines)
			{
				Match match;

				if (RegexUtils.Matches(line, ID_REGEX, out match))
				{
					output.Id = match.Groups["id"].Value;
				}
				else if (RegexUtils.Matches(line, DATES_REGEX, out match))
				{
					output.Start = CalendarComponent.DateTimeFromString(match.Groups["start"].Value);
					output.End = CalendarComponent.DateTimeFromString(match.Groups["end"].Value);
					output.Dialable = match.Groups["dialable"].Value == "dialable";
					output.Public = match.Groups["public"].Value == "public";
				}
				else if (RegexUtils.Matches(line, ORGANIZER_REGEX, out match))
				{
					output.Organizer = match.Groups["organizer"].Value;
				}
				else if (RegexUtils.Matches(line, LOCATION_REGEX, out match))
				{
					output.Location = match.Groups["location"].Value;
				}
				else if (RegexUtils.Matches(line, SUBJECT_REGEX, out match))
				{
					output.Subject = match.Groups["subject"].Value;
				}
				else if (RegexUtils.Matches(line, MEETING_PASSWORD_REGEX, out match))
				{
					output.MeetingPassword = match.Groups["meetingpassword"].Value;
				}
				else if (RegexUtils.Matches(line, ATTENDEE_REGEX, out match))
				{
					string attendee = match.Groups["attendee"].Value;
					output.m_Attendees.Add(attendee);
				}
				else if (RegexUtils.Matches(line, DialingNumber.DIALING_NUMBER_REGEX, out match))
				{
					DialingNumber number = DialingNumber.FromString(line);
					output.m_DialingNumbers.Add(number);
				}
			}

			return output;
		}

		/// <summary>
		/// Updates this instance with the values from the given instance.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public bool Update(MeetingInfo instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			bool change = false;

			if (!instance.m_Attendees.ScrambledEquals(m_Attendees))
			{
				m_Attendees.Clear();
				m_Attendees.AddRange(instance.m_Attendees);
				change = true;
			}

			if (!instance.m_DialingNumbers.ScrambledEquals(m_DialingNumbers))
			{
				m_DialingNumbers.Clear();
				m_DialingNumbers.AddRange(instance.m_DialingNumbers);
				change = true;
			}

			if (instance.Id != Id)
			{
				Id = instance.Id;
				change = true;
			}

			if (instance.Start != Start)
			{
				Start = instance.Start;
				change = true;
			}

			if (instance.End != End)
			{
				End = instance.End;
				change = true;
			}

			if (instance.Dialable != Dialable)
			{
				Dialable = instance.Dialable;
				change = true;
			}

			if (instance.Public != Public)
			{
				Public = instance.Public;
				change = true;
			}

			if (instance.Organizer != Organizer)
			{
				Organizer = instance.Organizer;
				change = true;
			}

			if (instance.Location != Location)
			{
				Location = instance.Location;
				change = true;
			}

			if (instance.Subject != Subject)
			{
				Subject = instance.Subject;
				change = true;
			}

			if (instance.MeetingPassword != MeetingPassword)
			{
				MeetingPassword = instance.MeetingPassword;
				change = true;
			}

			return change;
		}

		public sealed class DialingNumber
		{
			// dialingnumber|video|91138739
			// dialingnumber|video|chris.van@profoundtech.onmicrosoft.com;gruu;opaque=app:conf:focus:id:WPBNWHOH|sip
			// dialingnumber|audio|48527
			public const string DIALING_NUMBER_REGEX = @"dialingnumber\|(?'video'audio|video)\|(?'number'[^|]*)\|?(?'protocol'[^\|]*)?";

			public bool Video { get; private set; }
			public string Number { get; private set; }
			public string Protocol { get; private set; }

			/// <summary>
			/// Parses the meeting info dialing number and returns a DialingNumber instance.
			/// </summary>
			/// <param name="line"></param>
			/// <returns></returns>
			public static DialingNumber FromString(string line)
			{
				if (line == null)
					throw new ArgumentNullException("line");

				Match match = Regex.Match(line, DIALING_NUMBER_REGEX);

				if (!match.Success)
					throw new ArgumentException("Unable to parse dialing number", "line");

				return new DialingNumber
				{
					Video = match.Groups["video"].Value == "video",
					Number = match.Groups["number"].Value,
					Protocol = match.Groups["protocol"].Value,
				};
			}
		}
	}
}