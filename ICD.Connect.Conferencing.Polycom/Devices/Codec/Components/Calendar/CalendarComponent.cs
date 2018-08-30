using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Calendar
{
	public sealed class CalendarComponent : AbstractPolycomComponent
	{
		public const string TODAY = "today";
		public const string TOMORROW = "tomorrow";

		private const string DATE_FORMAT = "yyyy-MM-dd:HH:mm";

		/// <summary>
		/// Raised when meetings are added/removed to/from the component.
		/// </summary>
		public event EventHandler OnMeetingsChanged;

		private readonly Dictionary<string, MeetingInfo> m_MeetingInfos;
		private readonly SafeCriticalSection m_MeetingInfosSection;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="codec"></param>
		public CalendarComponent(PolycomGroupSeriesDevice codec)
			: base(codec)
		{
			m_MeetingInfos = new Dictionary<string, MeetingInfo>();
			m_MeetingInfosSection = new SafeCriticalSection();

			Subscribe(Codec);

			Codec.RegisterRangeFeedback("calendarmeetings list", HandleCalendarMeetings);
			Codec.RegisterRangeFeedback("calendarmeetings info", HandleCalendarInfo);

			if (Codec.Initialized)
				Initialize();
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			OnMeetingsChanged = null;

			base.Dispose(disposing);
		}

		/// <summary>
		/// Called to initialize the component.
		/// </summary>
		protected override void Initialize()
		{
			base.Initialize();

			CalendarList(TODAY, TOMORROW);
		}

		#region Methods

		/// <summary>
		/// Gets the available meeting infos.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<MeetingInfo> GetMeetingInfos()
		{
			return m_MeetingInfosSection.Execute(() => m_MeetingInfos.Values.ToArray(m_MeetingInfos.Count));
		}

		/// <summary>
		/// Converts the DateTime to a Polycom date string.
		/// </summary>
		/// <param name="dateTime"></param>
		/// <returns></returns>
		public static string DateTimeToString(DateTime dateTime)
		{
			return dateTime.ToString(DATE_FORMAT);
		}

		/// <summary>
		/// Converts the Polycom date string to a DateTime.
		/// </summary>
		/// <param name="dateTime"></param>
		/// <returns></returns>
		public static DateTime DateTimeFromString(string dateTime)
		{
			return DateTime.ParseExact(dateTime, DATE_FORMAT, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Requests a list of meetings between the start and end dates.
		/// </summary>
		/// <param name="start"></param>
		/// <param name="end"></param>
		public void CalendarList(DateTime start, DateTime end)
		{
			string startString = DateTimeToString(start);
			string endString = DateTimeToString(end);

			CalendarList(startString, endString);
		}

		/// <summary>
		/// Requests a list of meetings between the start and end dates.
		/// </summary>
		/// <param name="start"></param>
		/// <param name="end"></param>
		public void CalendarList(string start, string end)
		{
			Codec.EnqueueCommand("calendarmeetings list {0} {1}", start, end);
		}

		/// <summary>
		/// Requests info for the given meeting id.
		/// </summary>
		/// <param name="meetingId"></param>
		public void CalendarInfo(string meetingId)
		{
			Codec.EnqueueCommand("calendarmeetings info {0}", meetingId);
		}

		#endregion

		#region Codec Callbacks

		/// <summary>
		/// Meetings are returned as a single line containing ID, start time, end time and name.
		/// 
		/// E.g.
		///		meeting|AAAlAGNvbmt.....tJAAAFcf13UAABA=|2018-08-30:12:30|2018-08-30:14:30|Chris VanLuvanee
		/// </summary>
		/// <param name="meetings"></param>
		private void HandleCalendarMeetings(IEnumerable<string> meetings)
		{
			string[] currentIds = meetings.Select(m => Meeting.FromString(m).Id).ToArray();
			
			throw new NotImplementedException();
		}

		/// <summary>
		/// Calendar info is returned one meeting at a time, but spread over multiple lines.
		/// 
		/// E.g.
		///		id|AAAlAGNvbmZyb29tQHByb2ZvdW5kdGVjaC5vbm1pY3Jvc29mdC5jb20BUQAICNYOC4d8QABGAAAAAKDEA4kOv71Is4nKeHnLHVUHAKtBwbBNzutGjFgfJdA+tJAAAAAAAQ0AAKtBwbBNzutGjFgfJdA+tJAAAFcf13UAABA=
		///		2018-08-30:12:30|2018-08-30:14:30|dialable|public
		///		organizer|Chris VanLuvanee
		///		location|Online meeting; ConfRoom
		///		subject|Chris VanLuvanee
		///		dialingnumber|video|91138739
		///		dialingnumber|video|chris.van@profoundtech.onmicrosoft.com;gruu;opaque=app:conf:focus:id:WPBNWHOH|sip
		///		meetingpassword|none
		///		attendee|Chris VanLuvanee
		///		attendee|Chris VanLuvanee
		///		attendee|chris.van@wedoresi.com
		///		attendee|chris.cameron@profoundtech.onmicrosoft.com
		/// </summary>
		/// <param name="meetingInfo"></param>
		private void HandleCalendarInfo(IEnumerable<string> meetingInfo)
		{
			MeetingInfo instance = MeetingInfo.FromString(meetingInfo);

			throw new NotImplementedException();
		}

		#endregion
	}
}
