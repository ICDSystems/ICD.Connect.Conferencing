using System;
using System.Linq;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Bookings;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Controls.Conference;
using NUnit.Framework;

namespace ICD.Connect.Conferencing.Cisco.Tests.Devices.Codec.Components.Bookings
{
	[TestFixture]
	public sealed class BookingTest
	{
		private static readonly object[] s_FromXmlTestCases =
		{
			// First test case
			new object[]
			{
				@"<Booking item=""1"" maxOccurrence=""n"">
    <Id>349073</Id>
    <Title>Test One Button</Title>
    <Agenda>testagenda</Agenda>
    <Privacy>Public</Privacy>
    <Organizer>
      <FirstName>Ruben</FirstName>
      <LastName>Gonzalez</LastName>
      <Email>rgonzalez@firstrepublic.com</Email>
      <Id>testid</Id>
    </Organizer>
    <Time>
      <StartTime>2018-09-05T01:30:00Z</StartTime>
      <StartTimeBuffer>0</StartTimeBuffer>
      <EndTime>2018-09-05T02:30:00Z</EndTime>
      <EndTimeBuffer>0</EndTimeBuffer>
    </Time>
    <MaximumMeetingExtension>0</MaximumMeetingExtension>
    <MeetingExtensionAvailability></MeetingExtensionAvailability>
    <BookingStatus>OK</BookingStatus>
    <BookingStatusMessage>testmessage</BookingStatusMessage>
    <Webex>
      <Enabled>True</Enabled>
      <Url>testurl</Url>
      <MeetingNumber>testnumber</MeetingNumber>
      <Password>testpassword</Password>
      <HostKey>testkey</HostKey>
      <DialInNumbers/>
    </Webex>
    <Encryption>BestEffort</Encryption>
    <Role>Master</Role>
    <Recording>Disabled</Recording>
    <DialInfo>
      <Calls>
	    <Call item=""1"" maxOccurrence=""n"">
          <Number>432@firstrepublic.com</Number>
          <Protocol>SIP</Protocol>
          <CallRate>4096</CallRate>
          <CallType>Video</CallType>
        </Call>
      </Calls>
      <ConnectMode>Manual</ConnectMode>
    </DialInfo>
  </Booking>",
				"349073",
				Guid.Empty,
				"Test One Button",
				"testagenda",
				Booking.ePrivacy.Public,
				"Ruben",
				"Gonzalez",
				"rgonzalez@firstrepublic.com",
				"testid",
				new DateTime(2018, 9, 5, 1, 30, 0),
				new DateTime(2018, 9, 5, 2, 30, 0),
				"testmessage",
				true,
				"testurl",
				"testnumber",
				"testpassword",
				"testkey",
				1,
				"432@firstrepublic.com",
				"SIP",
				4096,
				eCiscoCallType.Video
			},

			// Second test case
			new object[]
			{
				@"  <Booking item=""1"" maxOccurrence=""n"">
    <Id>webex-5</Id>
    <MeetingId>14182066-0d3f-61d4-86ee-6f29cc9dc1fe</MeetingId>
    <Title>Device Connect Test Part 2 </Title>
    <Agenda></Agenda>
    <Privacy>Private</Privacy>
    <Organizer>
      <FirstName>Eckard, Tyler</FirstName>
      <LastName></LastName>
      <Email></Email>
      <Id>22f3efda-5da7-4982-862f-68a7214aa097</Id>
    </Organizer>
    <Time>
      <StartTime>2021-07-21T15:25:00Z</StartTime>
      <StartTimeBuffer>300</StartTimeBuffer>
      <EndTime>2021-07-21T15:55:00Z</EndTime>
      <EndTimeBuffer>0</EndTimeBuffer>
    </Time>
    <MaximumMeetingExtension>30</MaximumMeetingExtension>
    <MeetingExtensionAvailability>Guaranteed</MeetingExtensionAvailability>
    <BookingStatus>OK</BookingStatus>
    <BookingStatusMessage></BookingStatusMessage>
    <Webex>
      <Enabled>False</Enabled>
      <Url></Url>
      <MeetingNumber></MeetingNumber>
      <Password></Password>
      <HostKey></HostKey>
      <DialInNumbers/>
    </Webex>
    <Encryption>BestEffort</Encryption>
    <Recording>Disabled</Recording>
    <DialInfo>
      <Calls>
        <Call item=""1"" maxOccurrence=""n"">
          <Number>1453630407@onemetlife.webex.com</Number>
        </Call>
      </Calls>
      <ConnectMode>OBTP</ConnectMode>
    </DialInfo>
    <Spark>
      <MeetingType>Scheduled</MeetingType>
      <Alert>True</Alert>
      <LocusActive>False</LocusActive>
      <Pending>False</Pending>
      <Participants/>
    </Spark>
  </Booking>",
				"webex-5",
				new Guid("14182066-0d3f-61d4-86ee-6f29cc9dc1fe"),
				"Device Connect Test Part 2 ",
				string.Empty,
				Booking.ePrivacy.Private,
				"Eckard, Tyler",
				string.Empty,
				string.Empty,
				"22f3efda-5da7-4982-862f-68a7214aa097",
				new DateTime(2021,07,21,15,25,00),
				new DateTime(2021,07,21,15,55,00),
				string.Empty,
				false,
				string.Empty,
				string.Empty,
				string.Empty,
				string.Empty,
				1,
				"1453630407@onemetlife.webex.com",
				null,
				0,
				eCiscoCallType.Unknown
			},

			// third test case
			new object[] {
				@"			<Booking item=""1"" maxOccurrence=""n"">
			<Id>webex-4</Id>
			<MeetingId>f0354e2e-df49-6512-b9c5-106fe687fa4f</MeetingId>
			<Title>Test OBTP</Title>
			<Agenda/>
			<Privacy>Private</Privacy>
			<Organizer>
				<FirstName>Tingen, Drew</FirstName>
				<LastName/>
				<Email/>
				<Id>7706bf94-5a5e-62cc-afe5-83bf1d7cd835</Id>
			</Organizer>
			<Time>
				<StartTime>2021-08-10T21:30:00Z</StartTime>
				<StartTimeBuffer>300</StartTimeBuffer>
				<EndTime>2021-08-10T22:00:00Z</EndTime>
				<EndTimeBuffer>0</EndTimeBuffer>
			</Time>
			<MaximumMeetingExtension>30</MaximumMeetingExtension>
			<MeetingExtensionAvailability>Guaranteed</MeetingExtensionAvailability>
			<BookingStatus>OK</BookingStatus>
			<BookingStatusMessage/>
			<Cancellable>False</Cancellable>
			<Webex>
				<Enabled>False</Enabled>
				<Url/>
				<MeetingNumber/>
				<Password/>
				<HostKey/>
				<DialInNumbers/>
			</Webex>
			<Encryption>BestEffort</Encryption>
			<Recording>Disabled</Recording>
			<DialInfo>
				<Calls>
					<Call item=""1"" maxOccurrence=""n"">
						<Number>1457559328@onemetlife.webex.com</Number>
						<Protocol>Spark</Protocol>
					</Call>
				</Calls>
				<ConnectMode>OBTP</ConnectMode>
			</DialInfo>
			<Spark>
				<MeetingType>Scheduled</MeetingType>
				<Alert>True</Alert>
				<LocusActive>False</LocusActive>
				<Pending>False</Pending>
				<Participants/>
			</Spark>
		</Booking>",
				"webex-4",
				new Guid("f0354e2e-df49-6512-b9c5-106fe687fa4f"),
				"Test OBTP",
				string.Empty,
				Booking.ePrivacy.Private,
				"Tingen, Drew",
				string.Empty,
				string.Empty,
				"7706bf94-5a5e-62cc-afe5-83bf1d7cd835",
				new DateTime(2021,08,10,21,30,00),
				new DateTime(2021,08,10,22,00,00),
				string.Empty,
				false,
				string.Empty,
				string.Empty,
				string.Empty,
				string.Empty,
				1,
				"1457559328@onemetlife.webex.com",
				"Spark",
				0,
				eCiscoCallType.Unknown

			}
		};


		[TestCaseSource(nameof(s_FromXmlTestCases))]
		public void FromXmlTest(string xml, string expectedId, Guid expectedMeetingId, string expectedTitle, string expectedAgenda, Booking.ePrivacy epectedPrivacy,
		                        string expectedFirstName, string expectedLastName, string expectedEmail, string expectedOrganizerId,
		                        DateTime expectedStartTime, DateTime expectedEndTime, string expectedStatusMessage, bool expectedWebexEnabled,
		                        string expectedWebexUrl, string expectedWebexNumber, string expectedWebexPassword, string expectedWebexKey,
		                        int expectedCallCount, string expectedCallNumber, string expectedCallProtocol, int expectedCallRate,
		                        eCiscoCallType expectedCallType)
		{
			Booking booking = Booking.FromXml(xml);

			Assert.AreEqual(expectedId, booking.Id, "Id wrong");
			Assert.AreEqual(expectedMeetingId, booking.MeetingId, "MeetingId wrong");
			Assert.AreEqual(expectedTitle, booking.Title, "Title wrong");
			Assert.AreEqual(expectedAgenda, booking.Agenda, "Agenda wrong");
			Assert.AreEqual(epectedPrivacy, booking.Privacy, "Privacy wrong");

			Assert.AreEqual(expectedFirstName, booking.OrganizerFirstName, "First Name wrong");
			Assert.AreEqual(expectedLastName, booking.OrganizerLastName, "Last Name wrong");
			Assert.AreEqual(expectedEmail, booking.OrganizerEmail, "email wrong");
			Assert.AreEqual(expectedOrganizerId, booking.OrganizerId, "OrganizerId wrong");

			Assert.AreEqual(expectedStartTime, booking.StartTime, "start time wrong");
			Assert.AreEqual(expectedEndTime, booking.EndTime, "end time wrong");

			Assert.AreEqual(expectedStatusMessage, booking.BookingStatusMessage, "Booking status message wrong");

			Assert.AreEqual(expectedWebexEnabled, booking.WebexEnabled, "Webex enabled wrong");
			Assert.AreEqual(expectedWebexUrl, booking.WebexUrl, "Webex url wrong");
			Assert.AreEqual(expectedWebexNumber, booking.WebexMeetingNumber, "Webex meeting number wrong");
			Assert.AreEqual(expectedWebexPassword, booking.WebexPassword, "Webex password wrong");
			Assert.AreEqual(expectedWebexKey, booking.WebexHostKey, "webex host key wrong");

			BookingCall[] bookingCalls = booking.GetCalls().ToArray();

			Assert.AreEqual(expectedCallCount, bookingCalls.Length, "Number of calls wrong");
			Assert.AreEqual(expectedCallNumber, bookingCalls[0].Number, "Call number wrong");
			Assert.AreEqual(expectedCallProtocol, bookingCalls[0].Protocol, "Call protocol wrong");
			Assert.AreEqual(expectedCallRate, bookingCalls[0].CallRate, "Call rate wrong");
			Assert.AreEqual(expectedCallType, bookingCalls[0].CiscoCallType, "Call type wrong");
		}

		private static readonly object[] s_FromXmlWithoutCallsTestCases =
		{
			new object[]
			{
				@"		<Booking item=""1"" maxOccurrence=""n"">
			<Id>131726</Id>
			<Title>Scheduled Meeting</Title>
			<Agenda/>
			<Privacy>Public</Privacy>
			<Organizer>
				<FirstName>Madison</FirstName>
				<LastName>Wydysh</LastName>
				<Email>madison.wydysh@metlife.com</Email>
			</Organizer>
			<Time>
				<StartTime>2019-05-15T19:10:00Z</StartTime>
				<StartTimeBuffer>300</StartTimeBuffer>
				<EndTime>2019-05-15T19:20:00Z</EndTime>
				<EndTimeBuffer>0</EndTimeBuffer>
			</Time>
			<MaximumMeetingExtension>0</MaximumMeetingExtension>
			<BookingStatus>OK</BookingStatus>
			<BookingStatusMessage/>
			<Webex>
				<Enabled>True</Enabled>
				<Url>https://onemetlife.webex.com/onemetlife/j.php?MTID=m46a26d02b193a0e8bfe8da52b079727a</Url>
				<MeetingNumber>925090903</MeetingNumber>
				<Password>123456</Password>
			</Webex>
			<Encryption>BestEffort</Encryption>
			<Role>Master</Role>
			<Recording>Disabled</Recording>
			<DialInfo>
				<ConnectMode>Manual</ConnectMode>
			</DialInfo>
		</Booking>",
				"131726",
				Guid.Empty,
				"Scheduled Meeting",
				string.Empty,
				Booking.ePrivacy.Public,
				"Madison",
				"Wydysh",
				"madison.wydysh@metlife.com",
				null,
				new DateTime(2019, 5, 15, 19, 10, 0),
				new DateTime(2019, 5, 15, 19, 20, 0),
				string.Empty,
				true,
				"https://onemetlife.webex.com/onemetlife/j.php?MTID=m46a26d02b193a0e8bfe8da52b079727a",
				"925090903",
				"123456",
				null,
				0
			}
		};
		
		[TestCaseSource(nameof(s_FromXmlWithoutCallsTestCases))]
		public void FromXmlWithoutCallsTest(string xml, string expectedId, Guid expectedMeetindId, string expectedTitle, string expectedAgenda, Booking.ePrivacy epectedPrivacy,
		                                    string expectedFirstName, string expectedLastName, string expectedEmail, string expectedOrganizerId,
		                                    DateTime expectedStartTime, DateTime expectedEndTime, string expectedStatusMessage, bool expectedWebexEnabled,
		                                    string expectedWebexUrl, string expectedWebexNumber, string expectedWebexPassword, string expectedWebexKey,
		                                    int expectedCallCount)
		{
			Booking booking = Booking.FromXml(xml);

			Assert.AreEqual(expectedId, booking.Id);
			Assert.AreEqual(expectedMeetindId, booking.MeetingId);
			Assert.AreEqual(expectedTitle, booking.Title);
			Assert.AreEqual(expectedAgenda, booking.Agenda);
			Assert.AreEqual(epectedPrivacy, booking.Privacy);

			Assert.AreEqual(expectedFirstName, booking.OrganizerFirstName);
			Assert.AreEqual(expectedLastName, booking.OrganizerLastName);
			Assert.AreEqual(expectedEmail, booking.OrganizerEmail);
			Assert.AreEqual(expectedOrganizerId, booking.OrganizerId);

			Assert.AreEqual(expectedStartTime, booking.StartTime);
			Assert.AreEqual(expectedEndTime, booking.EndTime);

			Assert.AreEqual(expectedStatusMessage, booking.BookingStatusMessage);

			Assert.AreEqual(expectedWebexEnabled, booking.WebexEnabled);
			Assert.AreEqual(expectedWebexUrl, booking.WebexUrl);
			Assert.AreEqual(expectedWebexNumber, booking.WebexMeetingNumber);
			Assert.AreEqual(expectedWebexPassword, booking.WebexPassword);
			Assert.AreEqual(expectedWebexKey, booking.WebexHostKey);

			BookingCall[] bookingCalls = booking.GetCalls().ToArray();
			Assert.AreEqual(expectedCallCount, bookingCalls.Length);
		}
	}
}
