using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Server.Devices.Simpl;
using ICD.Connect.Devices.SPlusShims;
using ICD.Connect.Settings.SPlusShims.EventArguments;
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

		private ThinConferenceSource m_Source;

		private readonly SafeCriticalSection m_SourceCriticalSection;

		private string m_CallName;

		private string m_CallNumber;

		private eConferenceSourceAnswerState m_CallAnswerState;

		private eConferenceSourceDirection m_CallDirection;

		private eConferenceSourceStatus m_CallStatus;

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
				return m_Source == null ? (ushort)0 : (m_Source.Status == eConferenceSourceStatus.OnHold).ToUShort();
			}
			set
			{
				if (m_Source == null)
					return;

				m_Source.Status = value.ToBool() ? eConferenceSourceStatus.OnHold : eConferenceSourceStatus.Connected;

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
					m_Source.Name = name;
					m_Source.Number = number;
					m_Source.AnswerState = (eConferenceSourceAnswerState)answerState;
					m_Source.Direction = (eConferenceSourceDirection)direction;
					m_Source.Status = (eConferenceSourceStatus)status;
					m_Source.SourceType = eConferenceSourceType.Audio;
				}
				else
				{
					Originator.RemoveShimSource(m_Source);
					Unsubscribe(m_Source);
					m_Source = new ThinConferenceSource
					{
						Name = name,
						Number = number,
						AnswerState = (eConferenceSourceAnswerState)answerState,
						Direction = (eConferenceSourceDirection)direction,
						Status = (eConferenceSourceStatus)status,
						Start = IcdEnvironment.GetLocalTime(),
						SourceType = eConferenceSourceType.Audio
					};
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
					m_Source.Name = m_CallName;
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
					m_Source.Number = m_CallNumber;
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
				m_CallAnswerState = (eConferenceSourceAnswerState)answerState;
				if (m_Source != null)
					m_Source.AnswerState = m_CallAnswerState;
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
				m_CallDirection = (eConferenceSourceDirection)callDirection;
				if (m_Source != null)
					m_Source.Direction = m_CallDirection;
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
				m_CallStatus = (eConferenceSourceStatus)status;
				if (m_Source != null)
					m_Source.Status = m_CallStatus;
				else
				{
					// Create a new source if null
					Originator.RemoveShimSource(m_Source);
					Unsubscribe(m_Source);
					m_Source = new ThinConferenceSource
					{
						Name = m_CallName,
						Number = m_CallNumber,
						AnswerState = m_CallAnswerState,
						Direction = m_CallDirection,
						Status = m_CallStatus,
						Start = IcdEnvironment.GetLocalTime(),
						SourceType = eConferenceSourceType.Audio
					};
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
				m_CallAnswerState = eConferenceSourceAnswerState.Unknown;
				m_CallDirection = eConferenceSourceDirection.Undefined;
				m_CallStatus = eConferenceSourceStatus.Undefined;
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