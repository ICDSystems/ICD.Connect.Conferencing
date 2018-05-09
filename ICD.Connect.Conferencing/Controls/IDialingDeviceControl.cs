using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.API.Attributes;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Proxies;
using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Conferencing.Controls
{
	/// <summary>
	/// IDialingProvider provides an interface for managing conferences.
	/// </summary>
	[ApiClass(typeof(ProxyDialingDeviceControl), typeof(IDeviceControl))]
	public interface IDialingDeviceControl : IDeviceControl
	{
		/// <summary>
		/// Called when a source is added to the dialing component.
		/// </summary>
		event EventHandler<ConferenceSourceEventArgs> OnSourceAdded;

		/// <summary>
		/// Called when a source is removed from the dialing component.
		/// </summary>
		event EventHandler<ConferenceSourceEventArgs> OnSourceRemoved;

		/// <summary>
		/// Called when a source on this dialer dialing component state.
		/// </summary>
		event EventHandler<ConferenceSourceEventArgs> OnSourceChanged;

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
		eConferenceSourceType Supports { get; }

		#endregion

		#region Methods

		/// <summary>
		/// Gets the active conference sources.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		IEnumerable<IConferenceSource> GetSources();

		/// <summary>
		/// Dials the given number.
		/// </summary>
		/// <param name="number"></param>
		[ApiMethod(DialingDeviceControlApi.METHOD_DIAL, DialingDeviceControlApi.HELP_METHOD_DIAL)]
		void Dial(string number);

		/// <summary>
		/// Dials the given number.
		/// </summary>
		/// <param name="number"></param>
		/// <param name="callType"></param>
		[ApiMethod(DialingDeviceControlApi.METHOD_DIAL_TYPE, DialingDeviceControlApi.HELP_METHOD_DIAL_TYPE)]
		void Dial(string number, eConferenceSourceType callType);

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
}
