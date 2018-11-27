using System;
using System.Collections.Generic;
using ICD.Connect.Calendaring.Booking;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Devices;

namespace ICD.Connect.Conferencing.Mock
{
	public interface IMockConferencingDevice : IDevice
	{
		event EventHandler<ConferenceSourceEventArgs> OnSourceAdded;
		event EventHandler<ConferenceSourceEventArgs> OnSourceRemoved;

		IEnumerable<IConferenceSource> GetSources();

		void Dial(string number, eConferenceSourceType type);

		eBookingSupport CanDial(IBookingNumber bookingNumber);

		void Dial(IBookingNumber bookingNumber);
	}
}
