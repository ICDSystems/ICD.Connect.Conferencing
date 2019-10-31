using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Services;
using ICD.Connect.Conferencing.ConferencePoints;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.Contacts;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.DialingPlans;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Favorites;
using ICD.Connect.Conferencing.Participants;
using ICD.Connect.Devices;
using ICD.Connect.Settings.Cores;

namespace ICD.Connect.Conferencing.ConferenceManagers
{
	/// <summary>
	/// The ConferenceManager contains an IDialingPlan and a collection of IConferenceDeviceControls
	/// to place calls and manage the active conferences.
	/// </summary>
	public interface IConferenceManager
	{
		#region Events

		/// <summary>
		/// Raised when a new conference is instantiated and becomes active.
		/// </summary>
		event EventHandler<BoolEventArgs> OnIsAuthoritativeChanged;

		/// <summary>
		/// Raised when a participant is added to the current active conference.
		/// </summary>
		event EventHandler<ParticipantEventArgs> OnRecentParticipantAdded;

		/// <summary>
		/// Raised when the active conference changes.
		/// </summary>
		event EventHandler<ConferenceEventArgs> OnConferenceAdded;

		/// <summary>
		/// Raised when the active conference ends.
		/// </summary>
		event EventHandler<ConferenceEventArgs> OnConferenceRemoved;

		/// <summary>
		/// Called when the active conference status changes.
		/// </summary>
		event EventHandler<ConferenceStatusEventArgs> OnActiveConferenceStatusChanged;

		/// <summary>
		/// Called when an active participant status changes.
		/// </summary>
		event EventHandler<ParticipantStatusEventArgs> OnActiveParticipantStatusChanged;

		/// <summary>
		/// Raised when the privacy mute status changes.
		/// </summary>
		event EventHandler<BoolEventArgs> OnPrivacyMuteStatusChange;

		/// <summary>
		/// Raises when the in call state changes.
		/// </summary>
		event EventHandler<InCallEventArgs> OnInCallChanged;

		/// <summary>
		/// Raises when the conference adds or removes a participant.
		/// </summary>
		event EventHandler OnConferenceParticipantAddedOrRemoved;

		event EventHandler<ConferenceProviderEventArgs> OnProviderAdded;

		event EventHandler<ConferenceProviderEventArgs> OnProviderRemoved;

		#endregion

		#region Properties

		/// <summary>
		/// When true the conference manager will force registered dialers to match
		/// the state of the Privacy Mute, Do Not Disturb and Auto Answer properties.
		/// </summary>
		bool IsAuthoritative { get; set; }

		/// <summary>
		/// Gets the dialing plan.
		/// </summary>
		DialingPlan DialingPlan { get; }

		/// <summary>
		/// Gets the favorites.
		/// </summary>
		IFavorites Favorites { get; set; }

		/// <summary>
		/// Gets the active conference.
		/// </summary>
		IEnumerable<IConference> ActiveConferences { get; }

		IEnumerable<IConference> OnlineConferences { get; }

		/// <summary>
		/// Gets the AutoAnswer state.
		/// </summary>
		bool AutoAnswer { get; }

		/// <summary>
		/// Gets the current microphone mute state.
		/// </summary>
		bool PrivacyMuted { get; }

		/// <summary>
		/// Gets the DoNotDisturb state.
		/// </summary>
		bool DoNotDisturb { get; }

		/// <summary>
		/// Returns the current call state.
		/// </summary>
		eInCall IsInCall { get; }

		/// <summary>
		/// Gets the number of registered dialling providers.
		/// </summary>
		int DialingProvidersCount { get; }

		#endregion

		#region Methods

		/// <summary>
		/// Dials the given context.
		/// </summary>
		/// <param name="context"></param>
		void Dial(IDialContext context);

		/// <summary>
		/// Enables DoNotDisturb.
		/// </summary>
		/// <param name="state"></param>
		void EnableDoNotDisturb(bool state);

		/// <summary>
		/// Enables AutoAnswer.
		/// </summary>
		/// <param name="state"></param>
		void EnableAutoAnswer(bool state);

		/// <summary>
		/// Enabled privacy mute.
		/// </summary>
		/// <param name="state"></param>
		void EnablePrivacyMute(bool state);

		/// <summary>
		/// Gets the recent participants in order of time.
		/// </summary>
		/// <returns></returns>
		IEnumerable<IParticipant> GetRecentParticipants();

		/// <summary>
		/// Gets the registered conference components.
		/// </summary>
		/// <returns></returns>
		IEnumerable<IConferenceDeviceControl> GetDialingProviders();

		/// <summary>
		/// Gets the registered conference components.
		/// </summary>
		/// <param name="callType"></param>
		/// <returns></returns>
		IEnumerable<IConferenceDeviceControl> GetDialingProviders(eCallType callType);

		/// <summary>
		/// Registers the conference component.
		/// </summary>
		/// <param name="conferenceControl"></param>
		/// <param name="callType"></param>
		/// <returns></returns>
		bool RegisterDialingProvider(IConferenceDeviceControl conferenceControl, eCallType callType);

		/// <summary>
		/// Registers the conference component, for feedback only.
		/// </summary>
		/// <param name="conferenceControl"></param>
		/// <returns></returns>
		bool RegisterFeedbackDialingProvider(IConferenceDeviceControl conferenceControl);

		/// <summary>
		/// Deregisters the conference component.
		/// </summary>
		/// <param name="conferenceControl"></param>
		/// <returns></returns>
		bool DeregisterDialingProvider(IConferenceDeviceControl conferenceControl);

		/// <summary>
		/// Deregisters the conference componet from the feedback only list.
		/// </summary>
		/// <param name="conferenceControl"></param>
		/// <returns></returns>
		bool DeregisterFeedbackDialingProvider(IConferenceDeviceControl conferenceControl);

		/// <summary>
		/// Deregisters all of the conference components.
		/// </summary>
		void ClearDialingProviders();

		#endregion
	}

	/// <summary>
	/// Extension methods for IConferenceManager.
	/// </summary>
	public static class ConferenceManagerExtensions
	{
		/// <summary>
		/// Registers the dialing provider at the given conference point.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="conferencePoint"></param>
		public static void RegisterDialingProvider(this IConferenceManager extends, IConferencePoint conferencePoint)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			if (conferencePoint == null)
				throw new ArgumentNullException("conferencePoint");

			IDeviceBase device = ServiceProvider.GetService<ICore>()
			                                    .Originators
			                                    .GetChild<IDeviceBase>(conferencePoint.DeviceId);

			IConferenceDeviceControl control = device.Controls.GetControl<IConferenceDeviceControl>(conferencePoint.ControlId);

			extends.RegisterDialingProvider(control, conferencePoint.Type);
		}

		/// <summary>
		/// Dials the given contact. Call type is taken from the dialing plan.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="contact"></param>
		public static void Dial(this IConferenceManager extends, IContact contact)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			if (contact == null)
				throw new ArgumentNullException("contact");

			IDialContext dialContext = extends.DialingPlan.GetDialContext(contact);
			if (dialContext == null)
				throw new ArgumentException("Contact has no dial contexts", "contact");

			extends.Dial(dialContext);
		}

		/// <summary>
		/// Redials the contact from the given conference participant.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="participant"></param>
		public static void Dial(this IConferenceManager extends, ITraditionalParticipant participant)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			if (participant == null)
				throw new ArgumentNullException("participant");

			extends.Dial(participant.Number, participant.CallType);
		}

		/// <summary>
		/// Dials the given number for the given call type.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="number"></param>
		/// <param name="callType"></param>
		private static void Dial(this IConferenceManager extends, string number, eCallType callType)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			if (number == null)
				throw new ArgumentNullException("number");
			
			if (callType == eCallType.Unknown)
				callType = extends.DialingPlan.DefaultCallType;

			extends.Dial(new TraditionalDialContext { DialString = number, CallType = callType });
		}

		/// <summary>
		/// Toggles the current privacy mute state.
		/// </summary>
		/// <param name="extends"></param>
		public static void TogglePrivacyMute(this IConferenceManager extends)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			extends.EnablePrivacyMute(!extends.PrivacyMuted);
		}
	}
}
