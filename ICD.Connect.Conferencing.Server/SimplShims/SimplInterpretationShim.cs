using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants.Enums;
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

		public delegate void SPlusDialerShimStringCallback(ICDPlatformString number);
		public delegate void SPlusDialerShimUshortCallback(ushort enabled);       
		public delegate void SPlusDialerShimActionCallback();

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
		public SPlusDialerShimStringCallback DialCallback { get; set; }

		[PublicAPI("S+")]
		public SPlusDialerShimUshortCallback SetAutoAnswerCallback { get; set; }

		[PublicAPI("S+")]
		public SPlusDialerShimUshortCallback SetDoNotDisturbCallback { get; set; }

		[PublicAPI("S+")]
		public SPlusDialerShimUshortCallback SetPrivacyMuteCallback { get; set; }

		[PublicAPI("S+")]
		public SPlusDialerShimActionCallback HoldCallCallback { get; set; }

		[PublicAPI("S+")]
		public SPlusDialerShimActionCallback ResumeCallCallback { get; set; }

		[PublicAPI("S+")]
		public SPlusDialerShimStringCallback SendDtmfCallback { get; set; }

		[PublicAPI("S+")]
		public SPlusDialerShimActionCallback EndCallCallback { get; set; }

		#endregion

		#region Private Members

		private ThinConference m_Conference;

		private readonly SafeCriticalSection m_SourceCriticalSection;

		private string m_CallName;

		private string m_CallNumber;

		private eCallDirection m_CallDirection;

		private eConferenceStatus m_CallStatus;

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
				return m_Conference == null ? (ushort)0 : (m_Conference.Status == eConferenceStatus.OnHold).ToUShort();
			}
			set
			{
				if (m_Conference == null)
					return;

				m_Conference.Status = value.ToBool() ? eConferenceStatus.OnHold : eConferenceStatus.Connected;

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
				if (m_Conference != null)
				{
					m_Conference.Name = name;
					m_Conference.Number = number;
					m_Conference.Direction = (eCallDirection)direction;
					m_Conference.Status = (eConferenceStatus)status;
					m_Conference.CallType = eCallType.Audio;
				}
				else
				{
					Originator.RemoveShimConference(m_Conference);
					Unsubscribe(m_Conference);
					m_Conference = new ThinConference
					{
						Name = name,
						Number = number,
						Direction = (eCallDirection)direction,
						Status = (eConferenceStatus)status,
						StartTime = IcdEnvironment.GetUtcTime(),
						CallType = eCallType.Audio
					};

					Subscribe(m_Conference);

					Originator.AddShimConference(m_Conference);
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
				if (m_Conference != null)
					m_Conference.Name = m_CallName;
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
				if (m_Conference != null)
					m_Conference.Number = m_CallNumber;
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
				if (m_Conference != null)
					m_Conference.Direction = m_CallDirection;
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
				m_CallStatus = (eConferenceStatus)status;
				if (m_Conference != null)
					m_Conference.Status = (m_CallStatus);
				else
				{
					// Create a new source if null
					Originator.RemoveShimConference(m_Conference);
					Unsubscribe(m_Conference);
					m_Conference = new ThinConference
					{
						Name = m_CallName,
						Number = m_CallNumber,
						Direction = m_CallDirection,
						Status = m_CallStatus,
						StartTime = IcdEnvironment.GetUtcTime(),
						CallType = eCallType.Audio
					};

					Subscribe(m_Conference);

					Originator.AddShimConference(m_Conference);
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
				if (m_Conference == null || Originator == null)
					return;

				Originator.RemoveShimConference(m_Conference);
				Unsubscribe(m_Conference);
				m_Conference = null;
				m_CallName = null;
				m_CallNumber = null;
				m_CallDirection = eCallDirection.Undefined;
				m_CallStatus = eConferenceStatus.Undefined;
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
			SPlusDialerShimStringCallback handler = DialCallback;
			if (handler != null)
				handler(number);
		}

		private void OriginatorDialTypeCallback(ISimplInterpretationDevice sender, string number, ushort type)
		{
			if (type == eCallType.Video.ToUShort())
				return;
			SPlusDialerShimStringCallback handler = DialCallback;
			if (handler != null)
				handler(number);
		}

		private void OriginatorSetAutoAnswerCallback(ISimplInterpretationDevice sender, ushort enabled)
		{
			SPlusDialerShimUshortCallback handler = SetAutoAnswerCallback;
			if (handler != null)
				handler(enabled);
		}

		private void OriginatorSetDoNotDisturbCallback(ISimplInterpretationDevice sender, ushort enabled)
		{
			SPlusDialerShimUshortCallback handler = SetDoNotDisturbCallback;
			if (handler != null)
				handler(enabled);
		}

		private void OriginatorSetPrivacyMuteCallback(ISimplInterpretationDevice sender, ushort enabled)
		{
			SPlusDialerShimUshortCallback handler = SetPrivacyMuteCallback;
			if (handler != null)
				handler(enabled);
		}

		#endregion

		#region Conference Callbacks

		private void Subscribe(ThinConference participant)
		{
			if (participant == null)
				return;

			participant.HoldCallback = ConferenceHoldCallback;
			participant.ResumeCallback = ConferenceResumeCallback;
			participant.SendDtmfCallback = ConferenceSendDtmfCallback;
			participant.LeaveConferenceCallback = ConferenceHangupCallback;
		}

		private void Unsubscribe(ThinConference participant)
		{
			if (participant == null)
				return;

			participant.HoldCallback = null;
			participant.ResumeCallback = null;
			participant.SendDtmfCallback = null;
			participant.LeaveConferenceCallback = null;
		}

		private void ConferenceHoldCallback(ThinConference thinConference)
		{
			SPlusDialerShimActionCallback handler = HoldCallCallback;
			if (handler != null)
				handler();
		}

		private void ConferenceResumeCallback(ThinConference thinConference)
		{
			SPlusDialerShimActionCallback handler = ResumeCallCallback;
			if (handler != null)
				handler();
		}

		private void ConferenceSendDtmfCallback(ThinConference thinConference, string data)
		{
			SPlusDialerShimStringCallback handler = SendDtmfCallback;
			if (handler != null)
				handler(data);
		}

		private void ConferenceHangupCallback(ThinConference thinConference)
		{
			SPlusDialerShimActionCallback handler = EndCallCallback;
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

			var source = m_Conference;
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