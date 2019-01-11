using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Attributes;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.Contacts;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;
using ICD.Connect.Conferencing.Proxies.Controls.Dialing;
using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Conferencing.Controls.Dialing
{
	/// <summary>
	/// IDialingProvider provides an interface for managing conferences.
	/// </summary>
	[ApiClass(typeof(ProxyTraditionalConferenceDeviceControl), typeof(IDeviceControl))]
	public interface IConferenceDeviceControl<T> : IConferenceDeviceControl where T : IConference
	{
		#region Methods

		/// <summary>
		/// Gets the active conference sources.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		new IEnumerable<T> GetConferences();

		#endregion
	}

	public interface IConferenceDeviceControl : IDeviceControl
	{
		/// <summary>
		/// Raised when an incoming call is added to the dialing control.
		/// </summary>
		event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallAdded;

		/// <summary>
		/// Raised when an incoming call is removed from the dialing control.
		/// </summary>
		event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallRemoved;

		/// <summary>
		/// Raised when a conference is added to the dialing control.
		/// </summary>
		event EventHandler<ConferenceEventArgs> OnConferenceAdded;

		/// <summary>
		/// Raised when a conference is removed from the dialing control.
		/// </summary>
		event EventHandler<ConferenceEventArgs> OnConferenceRemoved;

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

		#region Properties

		/// <summary>
		/// Gets the AutoAnswer state.
		/// </summary>
		[ApiProperty(DialingDeviceControlApi.PROPERTY_AUTO_ANSWER, DialingDeviceControlApi.HELP_PROPERTY_AUTO_ANSWER)]
		bool AutoAnswer { get; }

		/// <summary>
		/// Gets the current microphone mute state.
		/// </summary>
		[ApiProperty(DialingDeviceControlApi.PROPERTY_PRIVACY_MUTED, DialingDeviceControlApi.HELP_PROPERTY_PRIVACY_MUTED)]
		bool PrivacyMuted { get; }

		/// <summary>
		/// Gets the DoNotDisturb state.
		/// </summary>
		[ApiProperty(DialingDeviceControlApi.PROPERTY_DO_NOT_DISTURB, DialingDeviceControlApi.HELP_PROPERTY_DO_NOT_DISTURB)]
		bool DoNotDisturb { get; }

		/// <summary>
		/// Gets the type of conference this dialer supports.
		/// </summary>
		[ApiProperty(DialingDeviceControlApi.PROPERTY_SUPPORTS, DialingDeviceControlApi.HELP_PROPERTY_SUPPORTS)]
		eCallType Supports { get; }

		#endregion

		#region Methods

		/// <summary>
		/// Gets the active conference sources.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		IEnumerable<IConference> GetConferences();

		/// <summary>
		/// Returns the level of support the device has for the given booking.
		/// </summary>
		/// <param name="dialContext"></param>
		/// <returns></returns>
		[ApiMethod(DialingDeviceControlApi.METHOD_CAN_DIAL, DialingDeviceControlApi.HELP_METHOD_CAN_DIAL)]
		eDialContextSupport CanDial(IDialContext dialContext);

		/// <summary>
		/// Dials the given booking.
		/// </summary>
		/// <param name="dialContext"></param>
		[ApiMethod(DialingDeviceControlApi.METHOD_DIAL_CONTEXT, DialingDeviceControlApi.HELP_METHOD_DIAL_CONTEXT)]
		void Dial(IDialContext dialContext);

		/// <summary>
		/// Sets the do-not-disturb enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		[ApiMethod(DialingDeviceControlApi.METHOD_SET_DO_NOT_DISTURB, DialingDeviceControlApi.HELP_METHOD_SET_DO_NOT_DISTURB)]
		void SetDoNotDisturb(bool enabled);

		/// <summary>
		/// Sets the auto-answer enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		[ApiMethod(DialingDeviceControlApi.METHOD_SET_AUTO_ANSWER, DialingDeviceControlApi.HELP_METHOD_SET_AUTO_ANSWER)]
		void SetAutoAnswer(bool enabled);

		/// <summary>
		/// Sets the privacy mute enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		[ApiMethod(DialingDeviceControlApi.METHOD_SET_PRIVACY_MUTE, DialingDeviceControlApi.HELP_METHOD_SET_PRIVACY_MUTE)]
		void SetPrivacyMute(bool enabled);

		#endregion
	}

	public static class DialingDeviceControlExtensions
	{
		public static void Dial(this IConferenceDeviceControl control, IContact contact)
		{
			if (contact == null)
				throw new ArgumentNullException("contact");

			IEnumerable<IDialContext> contactDialContexts = contact.GetDialContexts().ToList();
			if (!contactDialContexts.Any())
				throw new InvalidOperationException(string.Format("No contact methods for contact {0}", contact.Name));

			var groupedAndSorted = contactDialContexts.ToLookup(dc => control.CanDial(dc))
				.Where(g => g.Key != eDialContextSupport.Unsupported)
				.OrderByDescending(g => g.Key);
			if(!groupedAndSorted.Any())
				throw new InvalidOperationException(string.Format("No contact methods for contact {0} that this control supports dialing", contact.Name));

			var dialContext = groupedAndSorted.First().First();
			control.Dial(dialContext);
		}

		public static IConference GetActiveConference(this IConferenceDeviceControl extends)
		{
			return
				extends.GetConferences()
					.FirstOrDefault(c => c.Status == eConferenceStatus.Connected);
		}

		public static T GetBestDialer<T>(this IEnumerable<T> dialers, IDialContext dialContext) where T : IConferenceDeviceControl
		{
			if (dialers == null)
				throw new ArgumentNullException("dialers");

			var bestGroup = dialers.GroupBy(d => d.CanDial(dialContext))
				.Where(g => g.Key != eDialContextSupport.Unsupported)
				.OrderByDescending(g => g.Key).FirstOrDefault();
			return bestGroup == null ? default(T) : bestGroup.FirstOrDefault();
		}
	}
}
