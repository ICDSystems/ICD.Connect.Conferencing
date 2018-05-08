using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Devices.Simpl;

namespace ICD.Connect.Conferencing.Server.Devices.Simpl
{
	public delegate void SimplDialerDialCallback(IInterpretationDevice sender, string number);
	public delegate void SimplDialerDialTypeCallback(IInterpretationDevice sender, string number, ushort type);
	public delegate void SimplDialerSetAutoAnswerCallback(IInterpretationDevice sender, ushort enabled);
	public delegate void SimplDialerSetDoNotDisturbCallback(IInterpretationDevice sender, ushort enabled);
	public delegate void SimplDialerSetPrivacyMuteCallback(IInterpretationDevice sender, ushort enabled);

	public interface IInterpretationDevice : ISimplDevice
	{
		event EventHandler<ConferenceSourceEventArgs> OnSourceAdded;
		event EventHandler<ConferenceSourceEventArgs> OnSourceRemoved;

		event EventHandler<BoolEventArgs> OnAutoAnswerChanged;
		event EventHandler<BoolEventArgs> OnDoNotDisturbChanged;
		event EventHandler<BoolEventArgs> OnPrivacyMuteChanged;

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
		void Dial(string number, eConferenceSourceType type);
		void SetAutoAnswer(bool enabled);
		void SetDoNotDisturb(bool enabled);
		void SetPrivacyMute(bool enabled);

		void AddShimSource(IConferenceSource source);
		void RemoveShimSource(IConferenceSource source);

		IEnumerable<IConferenceSource> GetSources();
		bool ContainsSource(IConferenceSource source);
	}
}
