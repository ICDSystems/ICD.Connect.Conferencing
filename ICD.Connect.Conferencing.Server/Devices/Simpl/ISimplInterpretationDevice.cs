using System;
using System.Collections.Generic;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;
using ICD.Connect.Devices.CrestronSPlus.Devices.SPlus;
using ICD.Connect.Settings.CrestronSPlus.SPlusShims.EventArguments;

namespace ICD.Connect.Conferencing.Server.Devices.Simpl
{
	public delegate void SimplDialerDialCallback(ISimplInterpretationDevice sender, string number);
	public delegate void SimplDialerDialTypeCallback(ISimplInterpretationDevice sender, string number, ushort type);
	public delegate void SimplDialerSetAutoAnswerCallback(ISimplInterpretationDevice sender, ushort enabled);
	public delegate void SimplDialerSetDoNotDisturbCallback(ISimplInterpretationDevice sender, ushort enabled);
	public delegate void SimplDialerSetPrivacyMuteCallback(ISimplInterpretationDevice sender, ushort enabled);

	public interface ISimplInterpretationDevice : ISPlusDevice
	{
		event EventHandler<ParticipantEventArgs> OnSourceAdded;
		event EventHandler<ParticipantEventArgs> OnSourceRemoved;

		event EventHandler<SPlusBoolEventArgs> OnAutoAnswerChanged;
		event EventHandler<SPlusBoolEventArgs> OnDoNotDisturbChanged;
		event EventHandler<SPlusBoolEventArgs> OnPrivacyMuteChanged;

		event EventHandler<SPlusUShortEventArgs> OnBoothIdChanged;
		event EventHandler<SPlusStringEventArgs> OnLanguageChanged;

		SimplDialerDialCallback DialCallback { get; set; }
        SimplDialerDialTypeCallback DialTypeCallback { get; set; }
		SimplDialerSetAutoAnswerCallback SetAutoAnswerCallback { get; set; }
		SimplDialerSetDoNotDisturbCallback SetDoNotDisturbCallback { get; set; }
		SimplDialerSetPrivacyMuteCallback SetPrivacyMuteCallback { get; set; }

		string Language { get; set; }
		ushort BoothId { get; set; }

		bool AutoAnswer { get; set; }
		bool DoNotDisturb { get; set; }
		bool PrivacyMute { get; set; }

		void Dial(string number);
		void Dial(string number, eCallType type);
		void SetAutoAnswer(bool enabled);
		void SetDoNotDisturb(bool enabled);
		void SetPrivacyMute(bool enabled);

		void AddShimSource(ITraditionalParticipant source);
		void RemoveShimSource(IParticipant source);

		IEnumerable<ITraditionalParticipant> GetSources();
		bool ContainsSource(IParticipant source);
	}
}
