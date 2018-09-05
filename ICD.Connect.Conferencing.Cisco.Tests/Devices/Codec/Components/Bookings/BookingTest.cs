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

			Assert.AreEqual(new DateTime(2018, 9, 5, 1, 30, 0), booking.StartTime);
			Assert.AreEqual(new DateTime(2018, 9, 5, 2, 30, 0), booking.EndTime);

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
			Assert.AreEqual(eCallType.Video, bookingCalls[0].CallType);
		}
    }
}
