using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
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
	/// The IConferenceManager contains an IDialingPlan and a collection of IDialingProviders
	/// to place calls and manage an active conference.
	/// </summary>
	public interface IConferenceManager
	{
		#region Events

		/// <summary>
		/// Raised when a source is added to the current active conference.
		/// </summary>
		event EventHandler<ParticipantEventArgs> OnRecentSourceAdded;

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
		/// Called when an active source status changes.
		/// </summary>
		event EventHandler<ParticipantStatusEventArgs> OnActiveSourceStatusChanged;

		/// <summary>
		/// Raised when the privacy mute status changes.
		/// </summary>
		event EventHandler<BoolEventArgs> OnPrivacyMuteStatusChange;

		/// <summary>
		/// Raises when the in call state changes.
		/// </summary>
		event EventHandler<InCallEventArgs> OnInCallChanged;

		/// <summary>
		/// Raises when the conference adds or removes a source.
		/// </summary>
		event EventHandler OnConferenceSourceAddedOrRemoved;

		#endregion

		#region Properties

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
		IConference ActiveConference { get; }

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
		/// Gets the recent sources in order of time.
		/// </summary>
		/// <returns></returns>
		IEnumerable<IParticipant> GetRecentSources();

		/// <summary>
		/// Gets the conference component for the given source type.
		/// </summary>
		/// <param name="sourceType"></param>
		/// <returns></returns>
		[CanBeNull]
		IConferenceDeviceControl GetDialingProvider(eCallType sourceType);

		/// <summary>
		/// Gets the registered conference components.
		/// </summary>
		/// <returns></returns>
		IEnumerable<IConferenceDeviceControl> GetDialingProviders();

		/// <summary>
		/// Registers the conference component.
		/// </summary>
		/// <param name="sourceType"></param>
		/// <param name="conferenceControl"></param>
		/// <returns></returns>
		bool RegisterDialingProvider(eCallType sourceType, IConferenceDeviceControl conferenceControl);

		/// <summary>
		/// Registers the conference component, for feedback only.
		/// </summary>
		/// <param name="conferenceControl"></param>
		/// <returns></returns>
		bool RegisterFeedbackDialingProvider(IConferenceDeviceControl conferenceControl);

		/// <summary>
		/// Deregisters the conference component.
		/// </summary>
		/// <param name="sourceType"></param>
		/// <returns></returns>
		bool DeregisterDialingProvider(eCallType sourceType);

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
		/// Gets the call type for the given number.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="number"></param>
		/// <returns></returns>
		public static eCallType GetCallType(this IConferenceManager extends, string number)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			// Gets the type the number resolves to
			eCallType type = extends.DialingPlan.GetSourceType(number);

			// Gets the best provider for that call type
			IConferenceDeviceControl provider = extends.GetDialingProvider(type);

			// Return the best available type we can handle the call as.
			eCallType providerType = provider == null ? eCallType.Unknown : provider.Supports;

			// If we don't know the call type use the provider default.
			if (type == eCallType.Unknown)
				return providerType;

			// Limit the type to what the provider can support.
			return providerType < type ? providerType : type;
		}

		/// <summary>
		/// Returns true if the active conference is connected.
		/// </summary>
		/// <param name="extends"></param>
		public static bool GetIsActiveConferenceOnline(this IConferenceManager extends)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			IConference active = extends.ActiveConference;
			return active != null && active.GetOnlineParticipants().Any();
		}

		/// <summary>
		/// Dials the given number. Call type is taken from the dialling plan.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="number"></param>
		public static void Dial(this IConferenceManager extends, string number)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			eCallType mode = extends.GetCallType(number);
			extends.Dial(number, mode);
		}

		/// <summary>
		/// Dials the given contact. Call type is taken from the dialling plan.
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
		/// Redials the contact from the given conference source.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="source"></param>
		public static void Dial(this IConferenceManager extends, ITraditionalParticipant source)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			if (source == null)
				throw new ArgumentNullException("source");

			extends.Dial(source.Number, source.SourceType);
		}

		private static void Dial(this IConferenceManager extends, string number, eCallType callType)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			if (number == null)
				throw new ArgumentNullException("source");

			
			if (callType == eCallType.Unknown)
				callType = extends.DialingPlan.DefaultSourceType;

			extends.Dial(new GenericDialContext { DialString = number, CallType = callType });
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
