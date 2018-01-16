using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Conferencing.Controls
{
	/// <summary>
	/// IDialingProvider provides an interface for managing conferences.
	/// </summary>
	public interface IDialingDeviceControl : IDeviceControl
	{
		/// <summary>
		/// Called when a source is added to the dialing component.
		/// </summary>
		event EventHandler<ConferenceSourceEventArgs> OnSourceAdded;

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
		[PublicAPI]
		bool AutoAnswer { get; }

		/// <summary>
		/// Gets the current microphone mute state.
		/// </summary>
		[PublicAPI]
		bool PrivacyMuted { get; }

		/// <summary>
		/// Gets the DoNotDisturb state.
		/// </summary>
		[PublicAPI]
		bool DoNotDisturb { get; }

		/// <summary>
		/// Gets the type of conference this dialer supports.
		/// </summary>
		[PublicAPI]
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
		[PublicAPI]
		void Dial(string number);

		/// <summary>
		/// Dials the given number.
		/// </summary>
		/// <param name="number"></param>
		/// <param name="callType"></param>
		[PublicAPI]
		void Dial(string number, eConferenceSourceType callType);

		/// <summary>
		/// Sets the do-not-disturb enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		[PublicAPI]
		void SetDoNotDisturb(bool enabled);

		/// <summary>
		/// Sets the auto-answer enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		[PublicAPI]
		void SetAutoAnswer(bool enabled);

		/// <summary>
		/// Sets the privacy mute enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		[PublicAPI]
		void SetPrivacyMute(bool enabled);

		#endregion
	}
}
