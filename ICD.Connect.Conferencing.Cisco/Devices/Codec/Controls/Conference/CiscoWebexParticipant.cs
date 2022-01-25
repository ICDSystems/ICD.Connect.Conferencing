using System;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Cameras;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Conference;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;
using ICD.Connect.Conferencing.Participants.Enums;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Controls.Conference
{
	public sealed class CiscoWebexParticipant : AbstractParticipant
	{		
		private readonly string m_WebexParticipantId;
		private readonly Action m_AdmitCallback;
		private readonly Action m_KickCallback;
		private readonly Action<bool> m_MuteCallback;
		private readonly Action<bool> m_HandPositionCallback;

		private bool m_IsCoHost;
		private bool m_IsPresenter;

		

		public override IRemoteCamera Camera { get { return null; } }

		public string WebexParticipantId { get { return m_WebexParticipantId; }}

		public bool IsCoHost
		{
			get { return m_IsCoHost; }
			private set
			{
				if (m_IsCoHost == value)
					return;

				m_IsCoHost = value;

				OnIsCoHostChanged.Raise(this, value);
			}
		}

		public bool IsPresenter
		{
			get { return m_IsPresenter; }
			private set
			{
				if (m_IsPresenter == value)
					return;

				m_IsPresenter = value;

				OnIsPresenterChanged.Raise(this, value);
			}
		}

		public event EventHandler<BoolEventArgs> OnIsCoHostChanged;
		public event EventHandler<BoolEventArgs> OnIsPresenterChanged;

		#region Constructor

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="admitCallback"></param>
		/// <param name="kickCallback"></param>
		/// <param name="muteCallback"></param>
		/// <param name="handPositionCallback"></param>
		public CiscoWebexParticipant(WebexParticipantInfo info, [NotNull] Action admitCallback, [NotNull] Action kickCallback,
		                             [NotNull] Action<bool> muteCallback, [NotNull] Action<bool> handPositionCallback)
		{
			if (admitCallback == null)
				throw new ArgumentNullException("admitCallback");
			if (kickCallback == null)
				throw new ArgumentNullException("kickCallback");
			if (muteCallback == null)
				throw new ArgumentNullException("muteCallback");
			if (handPositionCallback == null)
				throw new ArgumentNullException("handPositionCallback");


			m_AdmitCallback = admitCallback;
			m_KickCallback = kickCallback;
			m_MuteCallback = muteCallback;
			m_HandPositionCallback = handPositionCallback;

			m_WebexParticipantId = info.ParticipantId;
			IsSelf = info.IsSelf;
			UpdateInfo(info);

			CallType = eCallType.Audio | eCallType.Video;
			StartTime = IcdEnvironment.GetUtcTime();
			DialTime = IcdEnvironment.GetUtcTime();
			AnswerState = eCallAnswerState.Answered;

			SupportedParticipantFeatures = eParticipantFeatures.GetIsMuted |
			                               eParticipantFeatures.GetIsSelf |
			                               eParticipantFeatures.GetIsHost |
			                               eParticipantFeatures.Kick |
			                               eParticipantFeatures.SetMute;
		}

		#endregion

		#region Methods

		public override void Admit()
		{
			m_AdmitCallback();
		}

		public override void Kick()
		{
			m_KickCallback();
		}

		public override void Mute(bool mute)
		{
			m_MuteCallback(mute);
		}

		public override void SetHandPosition(bool raised)
		{
			if (!IsSelf)
				return;

			m_HandPositionCallback(raised);
		}

		#endregion

		#region Internal Methods

		internal void CanKickAndMute(bool value)
		{
			SupportedParticipantFeatures =
				SupportedParticipantFeatures.SetFlags(eParticipantFeatures.Kick & eParticipantFeatures.SetMute, value);
		}

		internal void UpdateInfo(WebexParticipantInfo info)
		{
			Name = info.DisplayName;
			Status = info.Status;
			IsMuted = info.AudioMute;
			IsHost = info.IsHost;
			HandRaised = info.HandRaised;
			IsCoHost = info.CoHost;
			IsPresenter = info.IsPresenter;

			if (EndTime != null && info.Status == eParticipantStatus.Disconnected)
				EndTime = IcdEnvironment.GetUtcTime();

			SupportedParticipantFeatures = SupportedParticipantFeatures.SetFlags(eParticipantFeatures.RaiseLowerHand, IsSelf);
		}

		#endregion
	}
}