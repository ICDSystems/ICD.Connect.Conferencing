using System;
using System.Linq;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Bookings;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Dialing;
using NUnit.Framework;

namespace ICD.Connect.Conferencing.Cisco.Tests.Devices.Codec.Components.Bookings
{
	[TestFixture]
	public sealed class BookingTest
	{
		[Test]
		public void FromXmlTest()
		{
			const string xml = @"<Booking item=""1"" maxOccurrence=""n"">
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
  </Booking>";

			Booking booking = Booking.FromXml(xml);

			Assert.AreEqual(349073, booking.Id);
			Assert.AreEqual("Test One Button", booking.Title);
			Assert.AreEqual("testagenda", booking.Agenda);
			Assert.AreEqual(Booking.ePrivacy.Public, booking.Privacy);

			Assert.AreEqual("Ruben", booking.OrganizerFirstName);
			Assert.AreEqual("Gonzalez", booking.OrganizerLastName);
			Assert.AreEqual("rgonzalez@firstrepublic.com", booking.OrganizerEmail);
			Assert.AreEqual("testid", booking.OrganizerId);

			Assert.AreEqual(new DateTime(2018, 9, 4, 21, 30, 0), booking.StartTime);
			Assert.AreEqual(new DateTime(2018, 9, 4, 22, 30, 0), booking.EndTime);

			Assert.AreEqual("testmessage", booking.BookingStatusMessage);

			Assert.AreEqual(true, booking.WebexEnabled);
			Assert.AreEqual("testurl", booking.WebexUrl);
			Assert.AreEqual("testnumber", booking.WebexMeetingNumber);
			Assert.AreEqual("testpassword", booking.WebexPassword);
			Assert.AreEqual("testkey", booking.WebexHostKey);

			BookingCall[] bookingCalls = booking.GetCalls().ToArray();

			Assert.AreEqual(1, bookingCalls.Length);
			Assert.AreEqual("432@firstrepublic.com", bookingCalls[0].Number);
			Assert.AreEqual("SIP", bookingCalls[0].Protocol);
			Assert.AreEqual(4096, bookingCalls[0].CallRate);
			Assert.AreEqual(eCiscoCallType.Video, bookingCalls[0].CiscoCallType);
		}

		[Test]
		public void FromXmlWithoutCallsTest()
		{
			const string xml = @"		<Booking item=""1"" maxOccurrence=""n"">
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
		</Booking>";

			Booking booking = Booking.FromXml(xml);

			Assert.AreEqual(131726, booking.Id);
			Assert.AreEqual("Scheduled Meeting", booking.Title);
			Assert.AreEqual(string.Empty, booking.Agenda);
			Assert.AreEqual(Booking.ePrivacy.Public, booking.Privacy);

			Assert.AreEqual("Madison", booking.OrganizerFirstName);
			Assert.AreEqual("Wydysh", booking.OrganizerLastName);
			Assert.AreEqual("madison.wydysh@metlife.com", booking.OrganizerEmail);
			Assert.AreEqual(null, booking.OrganizerId);

			Assert.AreEqual(new DateTime(2019, 5, 15, 15, 10, 0), booking.StartTime);
			Assert.AreEqual(new DateTime(2019, 5, 15, 15, 20, 0), booking.EndTime);

			Assert.AreEqual(string.Empty, booking.BookingStatusMessage);

			Assert.AreEqual(true, booking.WebexEnabled);
			Assert.AreEqual("https://onemetlife.webex.com/onemetlife/j.php?MTID=m46a26d02b193a0e8bfe8da52b079727a",
			                booking.WebexUrl);
			Assert.AreEqual("925090903", booking.WebexMeetingNumber);
			Assert.AreEqual("123456", booking.WebexPassword);
			Assert.AreEqual(null, booking.WebexHostKey);

			BookingCall[] bookingCalls = booking.GetCalls().ToArray();
			Assert.AreEqual(0, bookingCalls.Length);
		}
	}
}
