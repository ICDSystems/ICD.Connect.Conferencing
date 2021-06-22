using System;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.IncomingCalls;
using ICD.Connect.Devices;

namespace ICD.Connect.Conferencing.Devices
{
	public interface IDialerDevice : IDevice
	{
		/// <summary>
		/// Called when a participant is added to the dialing device.
		/// </summary>
		event EventHandler<ConferenceEventArgs> OnConferenceAdded;

		/// <summary>
		/// Called when a participant is removed from the dialing device.
		/// </summary>
		event EventHandler<ConferenceEventArgs> OnConferenceRemoved;

		/// <summary>
		/// Called when an incoming call is added to the dialing device.
		/// </summary>
		event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallAdded;

		/// <summary>
		/// Called when an incoming call is removed from the dialing device.
		/// </summary>
		event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallRemoved;

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

		eDialContextSupport CanDial(IDialContext dialContext);
		void Dial(IDialContext dialContext);
		void SetPrivacyMute(bool enabled);
		void SetAutoAnswer(bool enabled);
		void SetDoNotDisturb(bool enabled);
	}
}