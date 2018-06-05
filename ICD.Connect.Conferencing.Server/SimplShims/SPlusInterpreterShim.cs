using System;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Server.Devices.Simpl;
using ICD.Connect.Devices.SPlusShims;
#if SIMPLSHARP
using ICDPlatformString = Crestron.SimplSharp.SimplSharpString;
#else
using ICDPlatformString = System.String;
#endif

namespace ICD.Connect.Conferencing.Server.SimplShims
{

	public sealed class SPlusInterpreterShim : AbstractSPlusDeviceShim<ISimplInterpretationDevice>
	{

		public delegate void SPlusDialerShimSetLanguageCallback(ICDPlatformString language);
		public delegate void SPlusDialerShimSetBoothIdCallback(ushort boothId);

		public delegate void SPlusDialerShimDialCallback(ICDPlatformString number);
		public delegate void SPlusDialerShimSetAutoAnswerCallback(ushort enabled);
		public delegate void SPlusDialerShimSetDoNotDisturbCallback(ushort enabled);
		public delegate void SPlusDialerShimSetPrivacyMuteCallback(ushort enabled);

		public delegate void SPlusDialerShimAnswerCallback();
		public delegate void SPlusDialerShimSetHoldCallback();
		public delegate void SPlusDialerShimSetResumeCallback();
		public delegate void SPlusDialerShimSendDtmfCallback(ICDPlatformString data);
		public delegate void SPlusDialerShimEndCallCallback();

		#region Events
		//Events for S+
		[PublicAPI("S+")]
		public event EventHandler<StringEventArgs> OnLanguageChanged;

		[PublicAPI("S+")]
		public event EventHandler<UShortEventArgs> OnBoothIdChanged;

		[PublicAPI("S+")]
		public event EventHandler<UShortEventArgs> OnAutoAnswerChanged;

		[PublicAPI("S+")]
		public event EventHandler<UShortEventArgs> OnDoNotDisturbChanged;

		[PublicAPI("S+")]
		public event EventHandler<UShortEventArgs> OnPrivacyMuteChanged;

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
		public SPlusDialerShimAnswerCallback AnswerCallCallback { get; set; }

		[PublicAPI("S+")] 
		public SPlusDialerShimSetHoldCallback HoldCallCallback { get; set; }

		[PublicAPI("S+")]
		public SPlusDialerShimSetResumeCallback ResumeCallCallback { get; set; }

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
				ISimplInterpretationDevice originator = Originator;
				if (originator == null)
					return string.Empty;

				return originator.Language;
			}
			set
			{
				ISimplInterpretationDevice originator = Originator;
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
				ISimplInterpretationDevice originator = Originator;
				if (originator == null)
					return 0;

				return originator.BoothId;
			}
			set
			{
				ISimplInterpretationDevice originator = Originator;
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
				ISimplInterpretationDevice originator = Originator;
				if (originator == null)
					return 0;

				return originator.AutoAnswer.ToUShort();
			}
			set
			{
				ISimplInterpretationDevice originator = Originator;
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
				ISimplInterpretationDevice originator = Originator;
				if (originator == null)
					return 0;

				return originator.DoNotDisturb.ToUShort();
			}
			set
			{
				ISimplInterpretationDevice originator = Originator;
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
				ISimplInterpretationDevice originator = Originator;
				if (originator == null)
					return 0;

				return originator.PrivacyMute.ToUShort();
			}
			set
			{
				ISimplInterpretationDevice originator = Originator;
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
				m_Source.Direction = (eConferenceSourceDirection)direction;
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

		#region Private/Protected Methods

		/// <summary>
		/// Called when the originator is detached
		/// Do any actions needed to desyncronize
		/// </summary>
		protected override void DeinitializeOriginator()
		{
			base.DeinitializeOriginator();
			ClearCallInfo();
		}

		#endregion       

		#region Originator Callbacks

		protected override void Subscribe(ISimplInterpretationDevice originator)
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

		protected override void Unsubscribe(ISimplInterpretationDevice originator)
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

		private void OriginatorDialCallback(ISimplInterpretationDevice sender, string number)
		{
			SPlusDialerShimDialCallback handler = DialCallback;
			if (handler != null)
				handler(number);
		}

		private void OriginatorDialTypeCallback(ISimplInterpretationDevice sender, string number, ushort type)
		{
			if (type == eConferenceSourceType.Video.ToUShort())
				return;
			SPlusDialerShimDialCallback handler = DialCallback;
			if (handler != null)
				handler(number);
		}

		private void OriginatorSetAutoAnswerCallback(ISimplInterpretationDevice sender, ushort enabled)
		{
			SPlusDialerShimSetAutoAnswerCallback handler = SetAutoAnswerCallback;
			if (handler != null)
				handler(enabled);
		}

		private void OriginatorSetDoNotDisturbCallback(ISimplInterpretationDevice sender, ushort enabled)
		{
			SPlusDialerShimSetDoNotDisturbCallback handler = SetDoNotDisturbCallback;
			if (handler != null)
				handler(enabled);
		}

		private void OriginatorSetPrivacyMuteCallback(ISimplInterpretationDevice sender, ushort enabled)
		{
			SPlusDialerShimSetPrivacyMuteCallback handler = SetPrivacyMuteCallback;
			if (handler != null)
				handler(enabled);
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
			SPlusDialerShimAnswerCallback handler = AnswerCallCallback;
			if (handler != null)
				handler();
		}

		private void SourceHoldCallback(ThinConferenceSource thinConferenceSource)
		{
			SPlusDialerShimSetHoldCallback handler = HoldCallCallback;
			if (handler != null)
				handler();
		}

		private void SourceResumeCallback(ThinConferenceSource thinConferenceSource)
		{
			SPlusDialerShimSetResumeCallback handler = ResumeCallCallback;
			if (handler != null)
				handler();
		}

		private void SourceSendDtmfCallback(ThinConferenceSource thinConferenceSource, string data)
		{
			SPlusDialerShimSendDtmfCallback handler = SendDtmfCallback;
			if (handler != null)
				handler(data);
		}

		private void SourceHangupCallback(ThinConferenceSource thinConferenceSource)
		{
			SPlusDialerShimEndCallCallback handler = EndCallCallback;
			if (handler != null)
				handler();
		}

		#endregion
	}
}