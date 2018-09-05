using System;
using System.Text.RegularExpressions;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Calendar
{
	public sealed class Meeting
	{
		// meeting|AAAlAGNvbmt.....tJAAAFcf13UAABA=|2018-08-30:12:30|2018-08-30:14:30|Chris VanLuvanee
		private const string MEETING_REGEX = @"meeting\|(?'id'[^|]*)\|(?'start'[^|]*)\|(?'end'[^|]*)\|(?'subject'[^|]*)";

		public string Id { get; private set; }
		public DateTime Start { get; private set; }
		public DateTime End { get; private set; }
		public string Subject { get; private set; }

		/// <summary>
		/// Parses the meeting string and returns a Meeting instance.
		/// </summary>
		/// <param name="meeting"></param>
		/// <returns></returns>
		public static Meeting FromString(string meeting)
		{
			if (meeting == null)
				throw new ArgumentNullException("meeting");

			Match match = Regex.Match(meeting, MEETING_REGEX);

			if (!match.Success)
				throw new ArgumentException("Unable to parse meeting", "meeting");

			DateTime start = CalendarComponent.DateTimeFromString(match.Groups["start"].Value);
			DateTime end = CalendarComponent.DateTimeFromString(match.Groups["end"].Value);

			return new Meeting
			{
				Id = match.Groups["id"].Value,
				Start = start,
				End = end,
				Subject = match.Groups["subject"].Value
			};
		}
	}
}