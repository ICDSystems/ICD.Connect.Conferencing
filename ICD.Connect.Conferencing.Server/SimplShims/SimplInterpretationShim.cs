using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;
using ICD.Connect.Conferencing.Server.Devices.Simpl;
using ICD.Connect.Devices.CrestronSPlus.SPlusShims;
using ICD.Connect.Settings.CrestronSPlus.SPlusShims.EventArguments;
#if SIMPLSHARP
using ICDPlatformString = Crestron.SimplSharp.SimplSharpString;
#else
using ICDPlatformString = System.String;
#endif

namespace ICD.Connect.Conferencing.Server.SimplShims
{

	public sealed class SimplInterpretationShim : AbstractSPlusDeviceShim<ISimplInterpretationDevice>
	{

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
		public event EventHandler<SPlusStringEventArgs> OnLanguageChanged;

		[PublicAPI("S+")]
		public event EventHandler<SPlusUShortEventArgs> OnBoothIdChanged;

		[PublicAPI("S+")]
		public event EventHandler<SPlusUShortEventArgs> OnAutoAnswerChanged;

		[PublicAPI("S+")]
		public event EventHandler<SPlusUShortEventArgs> OnDoNotDisturbChanged;

		[PublicAPI("S+")]
		public event EventHandler<SPlusUShortEventArgs> OnPrivacyMuteChanged;

		[PublicAPI("S+")]
		public event EventHandler<SPlusUShortEventArgs> OnHoldChanged;

		[PublicAPI("S+")]
		public event EventHandler<SPlusStringEventArgs> OnDtmfSent;

		[PublicAPI("S+")]
		public event EventHandler OnCallEnded;

		#endregion

		#region Callbacks

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

		private ThinTraditionalParticipant m_Source;

		private readonly SafeCriticalSection m_SourceCriticalSection;

		private string m_CallName;

		private string m_CallNumber;

		private eCallAnswerState m_CallAnswerState;

		private eCallDirection m_CallDirection;

		private eParticipantStatus m_CallStatus;

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

				OnLanguageChanged.Raise(this, new SPlusStringEventArgs(originator.Language));
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

				OnBoothIdChanged.Raise(this, new SPlusUShortEventArgs(originator.BoothId));
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

				OnAutoAnswerChanged.Raise(this, new SPlusUShortEventArgs(originator.AutoAnswer.ToUShort()));
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

				OnDoNotDisturbChanged.Raise(this, new SPlusUShortEventArgs(originator.DoNotDisturb.ToUShort()));
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

				OnPrivacyMuteChanged.Raise(this, new SPlusUShortEventArgs(originator.PrivacyMute.ToUShort()));
			}
		}

		[PublicAPI("S+")]
		public ushort Hold
		{
			get
			{
				return m_Source == null ? (ushort)0 : (m_Source.Status == eParticipantStatus.OnHold).ToUShort();
			}
			set
			{
				if (m_Source == null)
					return;

				m_Source.SetStatus(value.ToBool() ? eParticipantStatus.OnHold : eParticipantStatus.Connected);

				OnHoldChanged.Raise(this, new SPlusUShortEventArgs(value));
			}
		}

		#endregion

		#region Constructor

		public SimplInterpretationShim()
		{
			m_SourceCriticalSection = new SafeCriticalSection();
		}

		#endregion

		#region Methods

		[PublicAPI("S+")]
		public void SetCallInfo(string name, string number, ushort answerState, ushort direction, ushort status)
		{
			m_SourceCriticalSection.Enter();
			try
			{
				if (m_Source != null)
				{
					m_Source.SetName(name);
					m_Source.SetNumber(number);
					m_Source.SetDirection((eCallDirection)direction);
					m_Source.SetStatus((eParticipantStatus)status);
					m_Source.SetCallType(eCallType.Audio);
				}
				else
				{
					Originator.RemoveShimSource(m_Source);
					Unsubscribe(m_Source);
					m_Source = new ThinTraditionalParticipant();

					m_Source.SetName(name);
					m_Source.SetNumber(number);
					m_Source.SetDirection((eCallDirection)direction);
					m_Source.SetStatus((eParticipantStatus)status);
					m_Source.SetStart(IcdEnvironment.GetUtcTime());
					m_Source.SetCallType(eCallType.Audio);

					Subscribe(m_Source);

					Originator.AddShimSource(m_Source);
				}
			}
			finally
			{
				m_SourceCriticalSection.Leave();
			}
		}

		[PublicAPI("S+")]
		public void SetCallName(string name)
		{
			if (String.IsNullOrEmpty(name))
				return;
			m_SourceCriticalSection.Enter();
			try
			{
				m_CallName = name;
				if (m_Source != null)
					m_Source.SetName(m_CallName);
			}
			finally
			{
				m_SourceCriticalSection.Leave();
			}
		}

		[PublicAPI("S+")]
		public void SetCallNumber(string number)
		{
			if (String.IsNullOrEmpty(number))
				return;
			m_SourceCriticalSection.Enter();
			try
			{
				m_CallNumber = number;
				if (m_Source != null)
					m_Source.SetNumber(m_CallNumber);
			}
			finally
			{
				m_SourceCriticalSection.Leave();
			}
		}

		[PublicAPI("S+")]
		public void SetCallAnswerState(ushort answerState)
		{
			m_SourceCriticalSection.Enter();
			try
			{
				m_CallAnswerState = (eCallAnswerState)answerState;
				//if (m_Source != null)
				//	m_Source.AnswerState = m_CallAnswerState;
				throw new NotImplementedException();
			}
			finally
			{
				m_SourceCriticalSection.Leave();
			}
		}

		[PublicAPI("S+")]
		public void SetCallDirection(ushort callDirection)
		{
			m_SourceCriticalSection.Enter();
			try
			{
				m_CallDirection = (eCallDirection)callDirection;
				if (m_Source != null)
					m_Source.SetDirection(m_CallDirection);
			}
			finally
			{
				m_SourceCriticalSection.Leave();
			}
		}

		[PublicAPI("S+")]
		public void SetCallStatus(ushort status)
		{
			m_SourceCriticalSection.Enter();
			try
			{
				m_CallStatus = (eParticipantStatus)status;
				if (m_Source != null)
					m_Source.SetStatus(m_CallStatus);
				else
				{
					// Create a new source if null
					Originator.RemoveShimSource(m_Source);
					Unsubscribe(m_Source);
					m_Source = new ThinTraditionalParticipant();
					m_Source.SetName(m_CallName);
					m_Source.SetNumber(m_CallNumber);
					m_Source.SetDirection(m_CallDirection);
					m_Source.SetStatus(m_CallStatus);
					m_Source.SetStart(IcdEnvironment.GetUtcTime());
					m_Source.SetCallType(eCallType.Audio);
					Subscribe(m_Source);

					Originator.AddShimSource(m_Source);
				}
			}
			finally
			{
				m_SourceCriticalSection.Leave();
			}
		}

		[PublicAPI("S+")]
		public void ClearCallInfo()
		{
			m_SourceCriticalSection.Enter();
			try
			{
				if (m_Source == null || Originator == null)
					return;

				Originator.RemoveShimSource(m_Source);
				Unsubscribe(m_Source);
				m_Source = null;
				m_CallName = null;
				m_CallNumber = null;
				m_CallAnswerState = eCallAnswerState.Unknown;
				m_CallDirection = eCallDirection.Undefined;
				m_CallStatus = eParticipantStatus.Undefined;
			}
			finally
			{
				m_SourceCriticalSection.Leave();
			}
			OnCallEnded.Raise(this);
		}

		[PublicAPI("S+")]
		public void DtmfSent(string data)
		{
			OnDtmfSent.Raise(this, new SPlusStringEventArgs(data));
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
			if (type == eCallType.Video.ToUShort())
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

		private void Subscribe(ThinTraditionalParticipant participant)
		{
			if (participant == null)
				return;

			participant.HoldCallback = ParticipantHoldCallback;
			participant.ResumeCallback = ParticipantResumeCallback;
			participant.SendDtmfCallback = ParticipantSendDtmfCallback;
			participant.HangupCallback = ParticipantHangupCallback;
		}

		private void Unsubscribe(ThinTraditionalParticipant participant)
		{
			if (participant == null)
				return;

			participant.HoldCallback = null;
			participant.ResumeCallback = null;
			participant.SendDtmfCallback = null;
			participant.HangupCallback = null;
		}

		private void ParticipantAnswerCallback(ThinTraditionalParticipant thinParticipant)
		{
			SPlusDialerShimAnswerCallback handler = AnswerCallCallback;
			if (handler != null)
				handler();
		}

		private void ParticipantHoldCallback(ThinTraditionalParticipant thinParticipant)
		{
			SPlusDialerShimSetHoldCallback handler = HoldCallCallback;
			if (handler != null)
				handler();
		}

		private void ParticipantResumeCallback(ThinTraditionalParticipant thinParticipant)
		{
			SPlusDialerShimSetResumeCallback handler = ResumeCallCallback;
			if (handler != null)
				handler();
		}

		private void ParticipantSendDtmfCallback(ThinTraditionalParticipant thinParticipant, string data)
		{
			SPlusDialerShimSendDtmfCallback handler = SendDtmfCallback;
			if (handler != null)
				handler(data);
		}

		private void ParticipantHangupCallback(ThinTraditionalParticipant thinParticipant)
		{
			SPlusDialerShimEndCallCallback handler = EndCallCallback;
			if (handler != null)
				handler();
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			var source = m_Source;
			if (source != null)
				yield return source;
		}

		private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
		{
			return base.GetConsoleNodes();
		}

		#endregion
	}
}