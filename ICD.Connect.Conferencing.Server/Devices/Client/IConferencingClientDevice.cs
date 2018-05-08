using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Devices;

namespace ICD.Connect.Conferencing.Server.Devices.Client
{
	public interface IConferencingClientDevice : IDevice
	{
		event EventHandler<BoolEventArgs> OnConnectedStateChanged;

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

		bool IsConnected { get; }
		bool IsInterpretationActive { get; }

		bool PrivacyMuted { get; }
		bool DoNotDisturb { get; }
		bool AutoAnswer { get; }

		void Dial(string number);
		void Dial(string number, eConferenceSourceType callType);
		void SetPrivacyMute(bool enabled);
		void SetAutoAnswer(bool enabled);
		void SetDoNotDisturb(bool enabled);
		
		IEnumerable<IConferenceSource> GetSources();
	}
}