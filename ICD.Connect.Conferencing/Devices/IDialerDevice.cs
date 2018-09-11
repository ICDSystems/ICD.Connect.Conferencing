using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Calendaring.Booking;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.Contacts;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Devices;

namespace ICD.Connect.Conferencing.Devices
{
	public interface IDialerDevice : IDevice
	{
		/// <summary>
		/// Called when a source is added to the dialing device.
		/// </summary>
		event EventHandler<ConferenceSourceEventArgs> OnSourceAdded;

		/// <summary>
		/// Called when a source is removed from the dialing device.
		/// </summary>
		event EventHandler<ConferenceSourceEventArgs> OnSourceRemoved;

		/// <summary>
		/// Raised when the Do Not Disturb state changes.
		/// </summary>
		event EventHandler<BoolEventArgs> OnDoNotDisturbChanged;

		/// <summary>
		/// Raised when the Auto Answer state changes.
		/// </summary>
		event EventHandler<BoolEventArgs> OnAutoAnswerChanged;

		/// <summary>
		/// Raised when the microphones mute state changes.
		/// </summary>
		event EventHandler<BoolEventArgs> OnPrivacyMuteChanged;

		bool PrivacyMuted { get; }
		bool DoNotDisturb { get; }
		bool AutoAnswer { get; }

		void Dial(string number);
		void Dial(string number, eConferenceSourceType callType);
		void Dial(IContact contact);
		eBookingSupport CanDial(IBooking booking);
		void Dial(IBooking booking);
		void SetPrivacyMute(bool enabled);
		void SetAutoAnswer(bool enabled);
		void SetDoNotDisturb(bool enabled);
		
		IEnumerable<IConferenceSource> GetSources();
	}
}