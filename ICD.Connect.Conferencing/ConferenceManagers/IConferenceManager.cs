using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.ConferenceManagers.Recents;
using ICD.Connect.Conferencing.ConferencePoints;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.Contacts;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.DialingPlans;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Favorites;
using ICD.Connect.Conferencing.Participants;

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
		/// Raised when the Is Active state changes.
		/// </summary>
		event EventHandler<BoolEventArgs> OnIsActiveChanged; 

		/// <summary>
		/// Raised when the enforcement setting for do not disturb changes
		/// </summary>
		event EventHandler<GenericEventArgs<eEnforceState>> OnEnforceDoNotDisturbChanged;

		/// <summary>
		/// Raised when the enforcement setting for auto answer changes
		/// </summary>
		event EventHandler<GenericEventArgs<eEnforceState>> OnEnforceAutoAnswerChanged; 

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

		/// <summary>
		/// Raised when a conference control is added to the manager.
		/// </summary>
		event EventHandler<ConferenceProviderEventArgs> OnProviderAdded;

		/// <summary>
		/// Raised when a conference control is removed from the manager.
		/// </summary>
		event EventHandler<ConferenceProviderEventArgs> OnProviderRemoved;

		/// <summary>
		/// Called when an incoming call is added by a conference control.
 		/// </summary>
		event EventHandler<ConferenceControlIncomingCallEventArgs> OnIncomingCallAdded;

		/// <summary>
		/// Called when an incoming call is removed by a conference control.
		/// </summary>
		event EventHandler<ConferenceControlIncomingCallEventArgs> OnIncomingCallRemoved;

		/// <summary>
		/// Raised when a recent call is added to or removed from the recent calls collection.
		/// </summary>
		event EventHandler<RecentCallEventArgs> OnRecentCallsChanged;

		#endregion

		#region Properties

		/// <summary>
		/// Gets the conference manager volume points collection.
		/// </summary>
		[NotNull]
		ConferenceManagerVolumePoints VolumePoints { get; }

		/// <summary>
		/// Indicates whether this conference manager should do anything. 
		/// True normally, false when the room that owns this conference manager has a parent combine room
		/// </summary>
		bool IsActive { get; set; }

		eEnforceState EnforceDoNotDisturb { get; set; }

		eEnforceState EnforceAutoAnswer { get; set; }

		/// <summary>
		/// Gets the dialing plan.
		/// </summary>
		[NotNull]
		DialingPlan DialingPlan { get; }

		/// <summary>
		/// Gets the favorites.
		/// </summary>
		[CanBeNull]
		IFavorites Favorites { get; set; }

		/// <summary>
		/// Gets the active conferences.
		/// </summary>
		[NotNull]
		IEnumerable<IConference> ActiveConferences { get; }

		/// <summary>
		/// Gets the online conferences.
		/// </summary>
		[NotNull]
		IEnumerable<IConference> OnlineConferences { get; }

		/// <summary>
		/// Gets the current microphone mute state.
		/// </summary>
		bool PrivacyMuted { get; }

		/// <summary>
		/// Returns the current call state.
		/// </summary>
		eInCall IsInCall { get; }

		/// <summary>
		/// Gets the number of registered dialing providers.
		/// </summary>
		int DialingProvidersCount { get; }

		#endregion

		#region Methods

		/// <summary>
		/// Resets the conference manager back to its initial state.
		/// </summary>
		void Clear();

		/// <summary>
		/// Dials the given context.
		/// </summary>
		/// <param name="context"></param>
		void Dial([NotNull] IDialContext context);

		/// <summary>
		/// Enabled privacy mute.
		/// </summary>
		/// <param name="state"></param>
		void EnablePrivacyMute(bool state);

		/// <summary>
		/// Gets the recent calls in order of time.
		/// </summary>
		/// <returns></returns>
		[NotNull]
		IEnumerable<IRecentCall> GetRecentCalls();

		#endregion

		#region Dialing Providers

		/// <summary>
		/// Gets the registered conference controls.
		/// </summary>
		/// <returns></returns>
		[NotNull]
		IEnumerable<IConferenceDeviceControl> GetDialingProviders();

		/// <summary>
		/// Gets the registered conference controls.
		/// </summary>
		/// <param name="callType"></param>
		/// <returns></returns>
		[NotNull]
		IEnumerable<IConferenceDeviceControl> GetDialingProviders(eCallType callType);

		/// <summary>
		/// Gets the registered feedback conference providers.
		/// </summary>
		/// <returns></returns>
		[NotNull]
		IEnumerable<IConferenceDeviceControl> GetFeedbackDialingProviders();

		/// <summary>
		/// Registers the conference control.
		/// </summary>
		/// <param name="conferenceControl"></param>
		/// <param name="callType"></param>
		/// <returns></returns>
		bool RegisterDialingProvider([NotNull] IConferenceDeviceControl conferenceControl, eCallType callType);

		/// <summary>
		/// Registers the conference control, for feedback only.
		/// </summary>
		/// <param name="conferenceControl"></param>
		/// <returns></returns>
		bool RegisterFeedbackDialingProvider([NotNull] IConferenceDeviceControl conferenceControl);

		/// <summary>
		/// Deregisters the conference control.
		/// </summary>
		/// <param name="conferenceControl"></param>
		/// <returns></returns>
		bool DeregisterDialingProvider([NotNull] IConferenceDeviceControl conferenceControl);

		/// <summary>
		/// Deregisters the conference control from the feedback only list.
		/// </summary>
		/// <param name="conferenceControl"></param>
		/// <returns></returns>
		bool DeregisterFeedbackDialingProvider([NotNull] IConferenceDeviceControl conferenceControl);

		/// <summary>
		/// Deregisters all of the conference controls.
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
		public static void RegisterDialingProvider([NotNull] this IConferenceManager extends, [NotNull] IConferencePoint conferencePoint)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			if (conferencePoint == null)
				throw new ArgumentNullException("conferencePoint");

			if (conferencePoint.Control == null)
				throw new ArgumentException("Conference point does not have a conference control");

			extends.RegisterDialingProvider(conferencePoint.Control, conferencePoint.Type);
		}

		/// <summary>
		/// Dials the given contact. Call type is taken from the dialing plan.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="contact"></param>
		public static void Dial([NotNull] this IConferenceManager extends, [NotNull] IContact contact)
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
		public static void Dial([NotNull] this IConferenceManager extends, [NotNull] ITraditionalParticipant participant)
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
		private static void Dial([NotNull] this IConferenceManager extends, [NotNull] string number, eCallType callType)
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
		public static void TogglePrivacyMute([NotNull] this IConferenceManager extends)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			extends.EnablePrivacyMute(!extends.PrivacyMuted);
		}
	}
}
