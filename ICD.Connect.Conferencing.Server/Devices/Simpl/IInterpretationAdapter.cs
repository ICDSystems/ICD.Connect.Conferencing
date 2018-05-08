using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Devices.Simpl;

namespace ICD.Connect.Conferencing.Server.Devices.Simpl
{
	public delegate void SimplDialerDialCallback(IInterpretationAdapter sender, string number);
	public delegate void SimplDialerDialTypeCallback(IInterpretationAdapter sender, string number, ushort type);
	public delegate void SimplDialerSetAutoAnswerCallback(IInterpretationAdapter sender, ushort enabled);
	public delegate void SimplDialerSetDoNotDisturbCallback(IInterpretationAdapter sender, ushort enabled);
	public delegate void SimplDialerSetPrivacyMuteCallback(IInterpretationAdapter sender, ushort enabled);

	public interface IInterpretationAdapter : ISimplDevice
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
