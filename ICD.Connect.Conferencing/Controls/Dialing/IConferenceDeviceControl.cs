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
using ICD.Connect.Conferencing.IncomingCalls;
using ICD.Connect.Conferencing.Proxies.Controls.Dialing;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Telemetry.Attributes;

namespace ICD.Connect.Conferencing.Controls.Dialing
{
	[Flags]
	public enum eConferenceFeatures
	{
		None = 0,

		/// <summary>
		/// Supports setting auto-answer.
		/// </summary>
		AutoAnswer = 1,

		/// <summary>
		/// Supports setting privacy mute.
		/// </summary>
		PrivacyMute = 2,

		/// <summary>
		/// Supports setting do-not-disturb.
		/// </summary>
		DoNotDisturb = 4,

		/// <summary>
		/// Supports setting camera mute state.
		/// </summary>
		CameraMute = 8,

		/// <summary>
		/// Supports holding and resuming active conferences.
		/// </summary>
		Hold = 16,

		/// <summary>
		/// Supports the use of dual-tone multi-frequency signaling in active conferences.
		/// </summary>
		Dtmf = 32
	}

	/// <summary>
	/// IDialingProvider provides an interface for managing conferences.
	/// </summary>
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

	[ApiClass(typeof(ProxyTraditionalConferenceDeviceControl), typeof(IDeviceControl))]
	[ExternalTelemetry("Conference Device", typeof(DialingDeviceExternalTelemetryProvider))]
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
		/// Raised when the call-in info for the conference control changes.
		/// </summary>
		event EventHandler<GenericEventArgs<IDialContext>> OnCallInInfoChanged; 

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

		/// <summary>
		/// Raised when the camera's enabled state changes.
		/// </summary>
		event EventHandler<BoolEventArgs> OnCameraEnabledChanged;

		/// <summary>
		/// Raised when the supported conference features change.
		/// </summary>
		[ApiEvent(ConferenceDeviceControlApi.EVENT_SUPPORTED_CONFERENCE_FEATURES_CHANGED,
			ConferenceDeviceControlApi.HELP_EVENT_SUPPORTED_CONFERENCE_FEATURES_CHANGED)]
		event EventHandler<ConferenceControlSupportedConferenceFeaturesChangedApiEventArgs> OnSupportedConferenceFeaturesChanged; 

		#region Support

		/// <summary>
		/// Returns the features that are supported by this conference control.
		/// </summary>
		[ApiProperty(ConferenceDeviceControlApi.PROPERTY_SUPPORTED_CONFERENCE_FEATURES,
			ConferenceDeviceControlApi.HELP_PROPERTY_SUPPORTED_CONFERENCE_FEATURES)]
		eConferenceFeatures SupportedConferenceFeatures { get; }

		/// <summary>
		/// Gets the type of conference this dialer supports.
		/// </summary>
		[ApiProperty(ConferenceDeviceControlApi.PROPERTY_SUPPORTS, ConferenceDeviceControlApi.HELP_PROPERTY_SUPPORTS)]
		eCallType Supports { get; }

		#endregion

		#region Properties

		/// <summary>
		/// Gets the call-in info for this conference control.
		/// </summary>
		IDialContext CallInInfo { get; }

		/// <summary>
		/// Gets the AutoAnswer state.
		/// </summary>
		[ApiProperty(ConferenceDeviceControlApi.PROPERTY_AUTO_ANSWER, ConferenceDeviceControlApi.HELP_PROPERTY_AUTO_ANSWER)]
		bool AutoAnswer { get; }

		/// <summary>
		/// Gets the current microphone mute state.
		/// </summary>
		[ApiProperty(ConferenceDeviceControlApi.PROPERTY_PRIVACY_MUTED, ConferenceDeviceControlApi.HELP_PROPERTY_PRIVACY_MUTED)]
		bool PrivacyMuted { get; }

		/// <summary>
		/// Gets the DoNotDisturb state.
		/// </summary>
		[ApiProperty(ConferenceDeviceControlApi.PROPERTY_DO_NOT_DISTURB, ConferenceDeviceControlApi.HELP_PROPERTY_DO_NOT_DISTURB)]
		bool DoNotDisturb { get; }

		/// <summary>
		/// Gets the current camera enabled state.
		/// </summary>
		[ApiProperty(ConferenceDeviceControlApi.PROPERTY_CAMERA_ENABLED, ConferenceDeviceControlApi.HELP_PROPERTY_CAMERA_ENABLED)]
		bool CameraEnabled { get; }

		#endregion

		#region Methods

		/// <summary>
		/// Gets the active conference sources.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		IEnumerable<IConference> GetConferences();

		/// <summary>
		/// Returns the level of support the device has for the given context.
		/// </summary>
		/// <param name="dialContext"></param>
		/// <returns></returns>
		[ApiMethod(ConferenceDeviceControlApi.METHOD_CAN_DIAL, ConferenceDeviceControlApi.HELP_METHOD_CAN_DIAL)]
		eDialContextSupport CanDial([NotNull] IDialContext dialContext);

		/// <summary>
		/// Dials the given context.
		/// </summary>
		/// <param name="dialContext"></param>
		[ApiMethod(ConferenceDeviceControlApi.METHOD_DIAL_CONTEXT, ConferenceDeviceControlApi.HELP_METHOD_DIAL_CONTEXT)]
		void Dial([NotNull] IDialContext dialContext);

		/// <summary>
		/// Sets the do-not-disturb enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		[ApiMethod(ConferenceDeviceControlApi.METHOD_SET_DO_NOT_DISTURB, ConferenceDeviceControlApi.HELP_METHOD_SET_DO_NOT_DISTURB)]
		void SetDoNotDisturb(bool enabled);

		/// <summary>
		/// Sets the auto-answer enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		[ApiMethod(ConferenceDeviceControlApi.METHOD_SET_AUTO_ANSWER, ConferenceDeviceControlApi.HELP_METHOD_SET_AUTO_ANSWER)]
		void SetAutoAnswer(bool enabled);

		/// <summary>
		/// Sets the privacy mute enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		[ApiMethod(ConferenceDeviceControlApi.METHOD_SET_PRIVACY_MUTE, ConferenceDeviceControlApi.HELP_METHOD_SET_PRIVACY_MUTE)]
		void SetPrivacyMute(bool enabled);

		/// <summary>
		/// Sets the camera enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		[ApiMethod(ConferenceDeviceControlApi.METHOD_SET_CAMERA_ENABLED, ConferenceDeviceControlApi.HELP_METHOD_SET_CAMERA_ENABLED)]
		void SetCameraEnabled(bool enabled);

		#endregion
	}

	public static class ConferenceDeviceControlExtensions
	{
		/// <summary>
		/// Dials the given contact.
		/// </summary>
		/// <param name="control"></param>
		/// <param name="contact"></param>
		public static void Dial([NotNull] this IConferenceDeviceControl control, [NotNull] IContact contact)
		{
			if (control == null)
				throw new ArgumentNullException("control");

			if (contact == null)
				throw new ArgumentNullException("contact");

			IEnumerable<IDialContext> contactDialContexts = contact.GetDialContexts().ToList();
			if (!contactDialContexts.Any())
				throw new InvalidOperationException(string.Format("No contact methods for contact {0}", contact.Name));

			IOrderedEnumerable<IGrouping<eDialContextSupport, IDialContext>> groupedAndSorted =
				contactDialContexts.ToLookup(dc => control.CanDial(dc))
				                   .Where(g => g.Key != eDialContextSupport.Unsupported)
				                   .OrderByDescending(g => g.Key);

			IGrouping<eDialContextSupport, IDialContext> group;
			if (!groupedAndSorted.TryFirst(out group))
				throw new InvalidOperationException(string.Format("No contact methods for contact {0} that this control supports dialing", contact.Name));

			IDialContext dialContext = group.First();
			control.Dial(dialContext);
		}

		/// <summary>
		/// Gets the first active conference.
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		[CanBeNull]
		public static IConference GetActiveConference([NotNull] this IConferenceDeviceControl extends)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			return extends.GetConferences().FirstOrDefault(c => c.IsActive());
		}
	}
}
