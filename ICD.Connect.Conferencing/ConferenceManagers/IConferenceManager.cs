using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.ConferenceManagers.History;
using ICD.Connect.Conferencing.Contacts;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.DialingPlans;
using ICD.Connect.Conferencing.EventArguments;
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
		/// Raised when the privacy mute status changes.
		/// </summary>
		event EventHandler<BoolEventArgs> OnPrivacyMuteStatusChange;

		#endregion

		#region Properties

		/// <summary>
		/// Gets the conference manager dialers collection.
		/// </summary>
		[NotNull]
		ConferenceManagerDialers Dialers { get; }

		[NotNull]
		ConferenceManagerHistory History { get; }

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

		/// <summary>
		/// Gets/sets the enforce do-not-disturb mode.
		/// </summary>
		eEnforceState EnforceDoNotDisturb { get; set; }

		/// <summary>
		/// Gets/sets the enforce auto answer mode.
		/// </summary>
		eEnforceState EnforceAutoAnswer { get; set; }

		/// <summary>
		/// Gets the dialing plan.
		/// </summary>
		[NotNull]
		DialingPlan DialingPlan { get; }

		/// <summary>
		/// Gets/sets the privacy mute state.
		/// </summary>
		bool PrivacyMuted { get; set; }

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
		/// Returns true if:
		/// All of the active conferences can be privacy muted
		/// Or
		/// There are DSP or microphone privacy volume points.
		/// </summary>
		/// <returns></returns>
		bool CanPrivacyMute();

		#endregion
	}

	/// <summary>
	/// Extension methods for IConferenceManager.
	/// </summary>
	public static class ConferenceManagerExtensions
	{
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

			extends.Dial(new DialContext { DialString = number, CallType = callType });
		}

		/// <summary>
		/// Toggles the current privacy mute state.
		/// </summary>
		/// <param name="extends"></param>
		public static void TogglePrivacyMute([NotNull] this IConferenceManager extends)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			extends.PrivacyMuted = !extends.PrivacyMuted;
		}
	}
}
