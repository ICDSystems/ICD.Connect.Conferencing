using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.Contacts;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.DialingPlans;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Favorites;

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
		/// Raised when the enforcement setting for privacy mute changes
		/// </summary>
		event EventHandler<BoolEventArgs> OnEnforcePrivacyMuteChanged;

		/// <summary>
		/// Raised when the enforcement setting for do not disturb changes
		/// </summary>
		event EventHandler<GenericEventArgs<eEnforceState>> OnEnforceDoNotDisturbChanged;

		/// <summary>
		/// Raised when the enforcement setting for auto answer changes
		/// </summary>
		event EventHandler<GenericEventArgs<eEnforceState>> OnEnforceAutoAnswerChanged; 

		/// <summary>
		/// Raised when a new conference is instantiated and becomes active.
		/// </summary>
		event EventHandler<ConferenceEventArgs> OnRecentConferenceAdded;

		/// <summary>
		/// Raised when a source is added to the current active conference.
		/// </summary>
		event EventHandler<ConferenceSourceEventArgs> OnRecentSourceAdded;

		/// <summary>
		/// Raised when the active conference changes.
		/// </summary>
		event EventHandler<ConferenceEventArgs> OnActiveConferenceChanged;

		/// <summary>
		/// Raised when the active conference ends.
		/// </summary>
		event EventHandler<ConferenceEventArgs> OnActiveConferenceEnded;

		/// <summary>
		/// Called when the active conference status changes.
		/// </summary>
		event EventHandler<ConferenceStatusEventArgs> OnActiveConferenceStatusChanged;

		/// <summary>
		/// Called when an active source status changes.
		/// </summary>
		event EventHandler<ConferenceSourceStatusEventArgs> OnActiveSourceStatusChanged;

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

		event EventHandler<ConferenceProviderEventArgs> OnProviderAdded;

		event EventHandler<ConferenceProviderEventArgs> OnProviderRemoved;

		#endregion

		#region Properties

		/// <summary>
		/// Indicates whether this conference manager should do anything. 
		/// True normally, false when the room that owns this conference manager has a parent combine room
		/// </summary>
		bool IsActive { get; set; }

		bool EnforcePrivacyMute { get; set; }

		eEnforceState EnforceDoNotDisturb { get; set; }

		eEnforceState EnforceAutoAnswer { get; set; }

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
		/// Dials the given number.
		/// </summary>
		/// <param name="number"></param>
		/// <param name="mode"></param>
		void Dial(string number, eConferenceSourceType mode);

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
		/// Gets the recent conferences in order of time.
		/// </summary>
		/// <returns></returns>
		IEnumerable<IConference> GetRecentConferences();

		/// <summary>
		/// Gets the recent sources in order of time.
		/// </summary>
		/// <returns></returns>
		IEnumerable<IConferenceSource> GetRecentSources();

		/// <summary>
		/// Gets the dialing component for the given source type.
		/// </summary>
		/// <param name="sourceType"></param>
		/// <returns></returns>
		[CanBeNull]
		IDialingDeviceControl GetDialingProvider(eConferenceSourceType sourceType);

		/// <summary>
		/// Gets the registered dialing components.
		/// </summary>
		/// <returns></returns>
		IEnumerable<IDialingDeviceControl> GetDialingProviders();

		/// <summary>
		///  Gets the dialing provider for the given source type, falling back when unable to find a dialer for the type.
		/// </summary>
		/// <param name="sourceType"></param>
		/// <returns></returns>
		IDialingDeviceControl GetBestDialingProvider(eConferenceSourceType sourceType);

		/// <summary>
		/// Registers the dialing component.
		/// </summary>
		/// <param name="sourceType"></param>
		/// <param name="dialingControl"></param>
		/// <returns></returns>
		bool RegisterDialingProvider(eConferenceSourceType sourceType, IDialingDeviceControl dialingControl);

		/// <summary>
		/// Registers the dialing component, for feedback only.
		/// </summary>
		/// <param name="dialingControl"></param>
		/// <returns></returns>
		bool RegisterFeedbackDialingProvider(IDialingDeviceControl dialingControl);

		/// <summary>
		/// Deregisters the dialing component.
		/// </summary>
		/// <param name="sourceType"></param>
		/// <returns></returns>
		bool DeregisterDialingProvider(eConferenceSourceType sourceType);

		/// <summary>
		/// Deregisters the dialing componet from the feedback only list.
		/// </summary>
		/// <param name="dialingControl"></param>
		/// <returns></returns>
		bool DeregisterFeedbackDialingProvider(IDialingDeviceControl dialingControl);

		/// <summary>
		/// Deregisters all of the dialing components.
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
		public static eConferenceSourceType GetCallType(this IConferenceManager extends, string number)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			// Gets the type the number resolves to
			eConferenceSourceType type = extends.DialingPlan.GetSourceType(number);

			// Gets the best provider for that call type
			IDialingDeviceControl provider = extends.GetBestDialingProvider(type);

			// Return the best available type we can handle the call as.
			eConferenceSourceType providerType = provider == null ? eConferenceSourceType.Unknown : provider.Supports;

			// If we don't know the call type use the provider default.
			if (type == eConferenceSourceType.Unknown)
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
			return active != null && active.GetOnlineSources().Length > 0;
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

			eConferenceSourceType mode = extends.GetCallType(number);
			extends.Dial(number, mode);
		}

		/// <summary>
		/// Dials the given contact method. Call type is taken from the dialling plan.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="contactMethod"></param>
		public static void Dial(this IConferenceManager extends, IContactMethod contactMethod)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			if (contactMethod == null)
				throw new ArgumentNullException("contactMethod");

			extends.Dial(contactMethod.Number);
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

			IContactMethod contactMethod = extends.DialingPlan.GetContactMethod(contact);
			if (contactMethod == null)
				throw new ArgumentException("Contact has no contact methods", "contact");

			extends.Dial(contactMethod);
		}

		/// <summary>
		/// Redials the contact from the given conference source.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="source"></param>
		public static void Dial(this IConferenceManager extends, IConferenceSource source)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			if (source == null)
				throw new ArgumentNullException("source");

			extends.Dial(source.Number, source.SourceType);
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
