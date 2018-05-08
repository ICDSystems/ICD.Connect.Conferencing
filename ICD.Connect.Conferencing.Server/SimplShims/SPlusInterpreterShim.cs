using System;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Server.Devices.Simpl;
using ICD.Connect.Devices.SPlusShims;

namespace ICD.Connect.Conferencing.Server.SimplShims
{
	public delegate void SPlusDialerShimSetLanguageCallback(object sender, string language);
	public delegate void SPlusDialerShimSetBoothIdCallback(object sender, ushort boothId);

	public delegate void SPlusDialerShimDialCallback(object sender, string number, ushort callType);
	public delegate void SPlusDialerShimSetAutoAnswerCallback(object sender, ushort enabled);
	public delegate void SPlusDialerShimSetDoNotDisturbCallback(object sender, ushort enabled);
	public delegate void SPlusDialerShimSetPrivacyMuteCallback(object sender, ushort enabled);

	public delegate void SPlusDialerShimAnswerCallback(object sender);
	public delegate void SPlusDialerShimSetHoldCallback(object sender, ushort enabled);
	public delegate void SPlusDialerShimSendDtmfCallback(object sender, string data);
	public delegate void SPlusDialerShimEndCallCallback(object sender);

	public sealed class SPlusInterpreterShim : AbstractSPlusDeviceShim<IInterpretationAdapter>
	{
		#region Events
		//Events for S+
		[PublicAPI("S+")]
		public event EventHandler<StringEventArgs> OnLanguageChanged;

		[PublicAPI("S+")]
		public event EventHandler<UShortEventArgs> OnBoothIdChanged;

		[PublicAPI("S+")]
		public event EventHandler<StringEventArgs> OnCallDialed;

		[PublicAPI("S+")]
		public event EventHandler<UShortEventArgs> OnAutoAnswerChanged;

		[PublicAPI("S+")]
		public event EventHandler<UShortEventArgs> OnDoNotDisturbChanged;

		[PublicAPI("S+")]
		public event EventHandler<UShortEventArgs> OnPrivacyMuteChanged;

		[PublicAPI("S+")]
		public event EventHandler OnCallAnswered;

		[PublicAPI("S+")]
		public event EventHandler<UShortEventArgs> OnHoldChanged;

		[PublicAPI("S+")]
		public event EventHandler<StringEventArgs> OnDtmfSent;

		[PublicAPI("S+")]
		public event EventHandler OnCallEnded;

		#endregion

		#region Callbacks

		[PublicAPI("S+")]
		public SPlusDialerShimSetLanguageCallback SetLanguageCallback { get; set; }

		[PublicAPI("S+")]
		public SPlusDialerShimSetBoothIdCallback SetBoothIdCallback { get; set; }

		[PublicAPI("S+")]
		public SPlusDialerShimDialCallback DialCallback { get; set; }

		[PublicAPI("S+")]
		public SPlusDialerShimSetAutoAnswerCallback SetAutoAnswerCallback { get; set; }

		[PublicAPI("S+")]
		public SPlusDialerShimSetDoNotDisturbCallback SetDoNotDisturbCallback { get; set; }

		[PublicAPI("S+")]
		public SPlusDialerShimSetPrivacyMuteCallback SetPrivacyMuteCallback { get; set; }

		[PublicAPI("S+")]
		public SPlusDialerShimAnswerCallback AnswerCallback { get; set; }

		[PublicAPI("S+")] 
		public SPlusDialerShimSetHoldCallback SetHoldCallback { get; set; }

		[PublicAPI("S+")]
		public SPlusDialerShimSendDtmfCallback SendDtmfCallback { get; set; }

		[PublicAPI("S+")]
		public SPlusDialerShimEndCallCallback EndCallCallback { get; set; }

		#endregion

		#region Private Members

		private ThinConferenceSource m_Source;

		#endregion

		#region Properties

		[PublicAPI("S+")]
		public string Language
		{
			get
			{
				IInterpretationAdapter originator = Originator;
				if (originator == null)
					return string.Empty;

				return originator.Language;
			}
			set
			{
				IInterpretationAdapter originator = Originator;
				if (originator == null)
					return;

				originator.Language = value;

				OnLanguageChanged.Raise(this, new StringEventArgs(originator.Language));
			}
		}

		[PublicAPI("S+")]
		public ushort BoothId
		{
			get
			{
				IInterpretationAdapter originator = Originator;
				if (originator == null)
					return 0;

				return originator.BoothId;
			}
			set
			{
				IInterpretationAdapter originator = Originator;
				if (originator == null)
					return;

				originator.BoothId = value;

				OnBoothIdChanged.Raise(this, new UShortEventArgs(originator.BoothId));
			}
		}

		[PublicAPI("S+")]
		public ushort AutoAnswer
		{
			get
			{
				IInterpretationAdapter originator = Originator;
				if (originator == null)
					return 0;

				return originator.AutoAnswer.ToUShort();
			}
			set
			{
				IInterpretationAdapter originator = Originator;
				if (originator == null)
					return;

				originator.AutoAnswer = value.ToBool();

				OnAutoAnswerChanged.Raise(this, new UShortEventArgs(originator.AutoAnswer.ToUShort()));
			}
		}

		[PublicAPI("S+")]
		public ushort DoNotDisturb
		{
			get
			{
				IInterpretationAdapter originator = Originator;
				if (originator == null)
					return 0;

				return originator.DoNotDisturb.ToUShort();
			}
			set
			{
				IInterpretationAdapter originator = Originator;
				if (originator == null)
					return;

				originator.DoNotDisturb = value.ToBool();

				OnDoNotDisturbChanged.Raise(this, new UShortEventArgs(originator.DoNotDisturb.ToUShort()));
			}
		}

		[PublicAPI("S+")]
		public ushort PrivacyMute
		{
			get
			{
				IInterpretationAdapter originator = Originator;
				if (originator == null)
					return 0;

				return originator.PrivacyMute.ToUShort();
			}
			set
			{
				IInterpretationAdapter originator = Originator;
				if (originator == null)
					return;

				originator.PrivacyMute = value.ToBool();

				OnPrivacyMuteChanged.Raise(this, new UShortEventArgs(originator.PrivacyMute.ToUShort()));
			}
		}

		[PublicAPI("S+")]
		public ushort Hold
		{
			get
			{
				if (m_Source == null)
					return 0;
				return (m_Source.Status == eConferenceSourceStatus.OnHold).ToUShort();
			}
			set
			{
				if (m_Source == null)
					return;

				if (value.ToBool())
					m_Source.Status = eConferenceSourceStatus.OnHold;
				else
					m_Source.Status = eConferenceSourceStatus.Connected;

				OnHoldChanged.Raise(this, new UShortEventArgs(value));
			}
		}
		
		#endregion

		#region Methods

		[PublicAPI("S+")]
		public void SetCallInfo(string name, string number, ushort answerState, ushort direction, ushort status)
		{
			if (m_Source != null)
			{
				m_Source.Name = name;
				m_Source.Number = number;
				m_Source.AnswerState = (eConferenceSourceAnswerState)answerState;
				m_Source.Status = (eConferenceSourceStatus)status;
			}
			else
			{
				Unsubscribe(m_Source);
				m_Source = new ThinConferenceSource
				{
					Name = name,
					Number = number,
					AnswerState = (eConferenceSourceAnswerState)answerState,
					Direction = (eConferenceSourceDirection)direction,
					Status = (eConferenceSourceStatus)status,
					Start = IcdEnvironment.GetLocalTime()
				};
				Subscribe(m_Source);

				Originator.AddShimSource(m_Source);

				switch (m_Source.Direction)
				{
					case eConferenceSourceDirection.Outgoing:
						OnCallDialed.Raise(this, new StringEventArgs(number));
						break;
					case eConferenceSourceDirection.Incoming:
						OnCallAnswered.Raise(this);
						break;
				}
			}
		}

		[PublicAPI("S+")]
		public void ClearCallInfo()
		{
			Originator.RemoveShimSource(m_Source);
			Unsubscribe(m_Source);
			m_Source = null;
			OnCallEnded.Raise(this);
		}

		[PublicAPI("S+")]
		public void DtmfSent(string data)
		{
			OnDtmfSent.Raise(this, new StringEventArgs(data));
		}

		#endregion

		#region Originator Callbacks

		protected override void Subscribe(IInterpretationAdapter originator)
		{
			base.Subscribe(originator);

			if (originator == null)
				return;

			originator.DialCallback += OriginatorDialCallback;
			originator.DialTypeCallback += OriginatorDialTypeCallback;
			originator.SetAutoAnswerCallback += OriginatorSetAutoAnswerCallback;
			originator.SetDoNotDisturbCallback += OriginatorSetDoNotDisturbCallback;
			originator.SetPrivacyMuteCallback += OriginatorSetPrivacyMuteCallback;
		}

		protected override void Unsubscribe(IInterpretationAdapter originator)
		{
			base.Subscribe(originator);

			if (originator == null)
				return;

			originator.DialCallback = null;
			originator.DialTypeCallback = null;
			originator.SetAutoAnswerCallback = null;
			originator.SetDoNotDisturbCallback = null;
			originator.SetPrivacyMuteCallback = null;
		}

		private void OriginatorDialCallback(IInterpretationAdapter sender, string number)
		{
			SPlusDialerShimDialCallback handler = DialCallback;
			if (handler != null)
				handler(this, number, eConferenceSourceType.Audio.ToUShort());
		}

		private void OriginatorDialTypeCallback(IInterpretationAdapter sender, string number, ushort type)
		{
			SPlusDialerShimDialCallback handler = DialCallback;
			if (handler != null)
				handler(this, number, type);
		}

		private void OriginatorSetAutoAnswerCallback(IInterpretationAdapter sender, ushort enabled)
		{
			SPlusDialerShimSetAutoAnswerCallback handler = SetAutoAnswerCallback;
			if (handler != null)
				handler(this, enabled);
		}

		private void OriginatorSetDoNotDisturbCallback(IInterpretationAdapter sender, ushort enabled)
		{
			SPlusDialerShimSetDoNotDisturbCallback handler = SetDoNotDisturbCallback;
			if (handler != null)
				handler(this, enabled);
		}

		private void OriginatorSetPrivacyMuteCallback(IInterpretationAdapter sender, ushort enabled)
		{
			SPlusDialerShimSetPrivacyMuteCallback handler = SetPrivacyMuteCallback;
			if (handler != null)
				handler(this, enabled);
		}

		#endregion

		#region Source Callbacks

		private void Subscribe(ThinConferenceSource source)
		{
			if (source == null)
				return;

			source.AnswerCallback = SourceAnswerCallback;
			source.HoldCallback = SourceHoldCallback;
			source.ResumeCallback = SourceResumeCallback;
			source.SendDtmfCallback = SourceSendDtmfCallback;
			source.HangupCallback = SourceHangupCallback;
		}

		private void Unsubscribe(ThinConferenceSource source)
		{
			if (source == null)
				return;

			source.AnswerCallback = null;
			source.HoldCallback = null;
			source.ResumeCallback = null;
			source.SendDtmfCallback = null;
			source.HangupCallback = null;
		}

		private void SourceAnswerCallback(ThinConferenceSource thinConferenceSource)
		{
			SPlusDialerShimAnswerCallback handler = AnswerCallback;
			if (handler != null)
				handler(this);
		}

		private void SourceHoldCallback(ThinConferenceSource thinConferenceSource)
		{
			SPlusDialerShimSetHoldCallback handler = SetHoldCallback;
			if (handler != null)
				handler(this, true.ToUShort());
		}

		private void SourceResumeCallback(ThinConferenceSource thinConferenceSource)
		{
			SPlusDialerShimSetHoldCallback handler = SetHoldCallback;
			if (handler != null)
				handler(this, false.ToUShort());
		}

		private void SourceSendDtmfCallback(ThinConferenceSource thinConferenceSource, string data)
		{
			SPlusDialerShimSendDtmfCallback handler = SendDtmfCallback;
			if (handler != null)
				handler(this, data);
		}

		private void SourceHangupCallback(ThinConferenceSource thinConferenceSource)
		{
			SPlusDialerShimEndCallCallback handler = EndCallCallback;
			if (handler != null)
				handler(this);
		}

		#endregion
	}
}