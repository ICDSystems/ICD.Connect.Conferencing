using System;
using System.Collections.Generic;
using ICD.Common.EventArguments;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.Contacts;
using ICD.Connect.Conferencing.Controls;
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
		/// Raised when a source is added or removed to the active conference.
		/// </summary>
		event EventHandler<ConferenceEventArgs> OnActiveConferenceSourcesChanged;

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
		/// Raises true when a call begins and false when all calls have ended.
		/// </summary>
		event EventHandler<BoolEventArgs> OnInCallChanged;

		/// <summary>
		/// Raises true when a video call begins and false when all video calls have ended.
		/// </summary>
		event EventHandler<BoolEventArgs> OnInVideoCallChanged;

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
		/// Returns true if actively in a video call.
		/// </summary>
		bool IsInVideoCall { get; }

		/// <summary>
		/// Returns true if actively in a call.
		/// </summary>
		bool IsInCall { get; }

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
		IDialingDeviceControl GetDialingProvider(eConferenceSourceType sourceType);

		/// <summary>
		/// Gets the registered dialing components.
		/// </summary>
		/// <returns></returns>
		IEnumerable<IDialingDeviceControl> GetDialingProviders();

		/// <summary>
		/// Registers the dialing component.
		/// </summary>
		/// <param name="sourceType"></param>
		/// <param name="dialingControl"></param>
		/// <returns></returns>
		bool RegisterDialingProvider(eConferenceSourceType sourceType, IDialingDeviceControl dialingControl);

		/// <summary>
		/// Deregisters the dialing component.
		/// </summary>
		/// <param name="sourceType"></param>
		/// <returns></returns>
		bool DeregisterDialingProvider(eConferenceSourceType sourceType);

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

			eConferenceSourceType mode = extends.DialingPlan.GetSourceType(number);
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
		/// Dials the given contact method.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="contactMethod"></param>
		/// <param name="mode"></param>
		public static void Dial(this IConferenceManager extends, IContactMethod contactMethod, eConferenceSourceType mode)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			if (contactMethod == null)
				throw new ArgumentNullException("contactMethod");

			extends.Dial(contactMethod.Number, mode);
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
			extends.Dial(contactMethod);
		}

		/// <summary>
		/// Dials the given contact, attempting to find a contact method that matches the mode.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="contact"></param>
		/// <param name="mode"></param>
		public static void Dial(this IConferenceManager extends, IContact contact, eConferenceSourceType mode)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			if (contact == null)
				throw new ArgumentNullException("contact");

			IContactMethod contactMethod = extends.DialingPlan.GetContactMethod(contact, mode);
			extends.Dial(contactMethod, mode);
		}

		/// <summary>
		/// Gets the dialing provider for the given number.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="number"></param>
		/// <returns></returns>
		public static IDialingDeviceControl GetDialingProvider(this IConferenceManager extends, string number)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			eConferenceSourceType sourceType = extends.DialingPlan.GetSourceType(number);
			return extends.GetDialingProvider(sourceType);
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
