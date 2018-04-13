using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Devices;

namespace ICD.Connect.Conferencing.Server
{
	public interface IConferencingClientDevice : IDevice
	{
		event EventHandler<BoolEventArgs> OnConnectedStateChanged;
		bool IsConnected { get; }
		bool PrivacyMuteEnabled { get; }
		bool HoldEnabled { get; }
		bool CallEnded { get; }
		void Dial(string number);
		void Dial(string number, eConferenceSourceType type);
		void SetPrivacyMute(bool enabled);
		void SetAutoAnswer(bool enabled);
		void SetDoNotDisturb(bool enabled);
		void HoldEnable();
		void HoldResume();
		void EndCall();
		void SendDtmf(string data);
		IEnumerable<IConferenceSource> GetSources();
	}
}