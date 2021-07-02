using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Xml;
using ICD.Connect.Conferencing.Cameras;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Conference;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;
using ICD.Connect.Conferencing.Participants.Enums;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Controls.Conference
{
	public sealed class CiscoWebexParticipant : AbstractParticipant
	{
		private WebexParticipantInfo m_Info;

		private readonly int m_CallId;
		private readonly ConferenceComponent m_ConferenceComponent;

		public override IRemoteCamera Camera { get { return null; } }

		public string WebexParticipantId { get { return m_Info.ParticipantId; } }

		public bool IsCoHost { get { return m_Info.CoHost; } }

		public bool IsPresenter { get { return m_Info.IsPresenter; } }

		#region Constructor

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="callId"></param>
		/// <param name="conferenceComponent"></param>
		public CiscoWebexParticipant(WebexParticipantInfo info, int callId, [NotNull] ConferenceComponent conferenceComponent)
		{
			if (conferenceComponent == null)
				throw new ArgumentNullException("conferenceComponent");

			IsSelf = info.IsSelf;
			UpdateInfo(info);
			m_CallId = callId;

			m_ConferenceComponent = conferenceComponent;
			Subscribe(m_ConferenceComponent);

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

		protected override void DisposeFinal()
		{
			Unsubscribe(m_ConferenceComponent);

			base.DisposeFinal();
		}

		#endregion

		#region Methods

		public override void Admit()
		{
			m_ConferenceComponent.ParticipantAdmit(m_CallId, m_Info.ParticipantId);
		}

		public override void Kick()
		{
			m_ConferenceComponent.ParticipantDisconnect(m_CallId, m_Info.ParticipantId);
		}

		public override void Mute(bool mute)
		{
			m_ConferenceComponent.ParticipantMute(mute, m_CallId, m_Info.ParticipantId);
		}

		public override void SetHandPosition(bool raised)
		{
			if (!IsSelf)
				return;

			if (raised)
				m_ConferenceComponent.RaiseHand(m_CallId);
			else
				m_ConferenceComponent.LowerHand(m_CallId);
		}

		#endregion

		#region Private Methods

		private void UpdateInfo(WebexParticipantInfo newInfo)
		{
			m_Info = newInfo;

			Name = m_Info.DisplayName;
			Status = m_Info.Status;
			IsMuted = m_Info.AudioMute;
			IsHost = m_Info.IsHost;
			HandRaised = m_Info.HandRaised;

			if (EndTime != null && m_Info.Status == eParticipantStatus.Disconnected)
				EndTime = IcdEnvironment.GetUtcTime();

			SupportedParticipantFeatures.SetFlags(eParticipantFeatures.RaiseLowerHand, IsSelf);
		}

		#endregion

		#region Conference Component Callbacks

		private void Subscribe(ConferenceComponent conferenceComponent)
		{
			conferenceComponent.OnWebexParticipantsListSearchResult += ConferenceComponentOnWebexParticipantsListSearchResult;
			conferenceComponent.OnWebexParticipantListUpdated += ConferenceComponentOnWebexParticipantListUpdated;
		}

		private void Unsubscribe(ConferenceComponent conferenceComponent)
		{
			conferenceComponent.OnWebexParticipantsListSearchResult -= ConferenceComponentOnWebexParticipantsListSearchResult;
			conferenceComponent.OnWebexParticipantListUpdated -= ConferenceComponentOnWebexParticipantListUpdated;
		}

		private void ConferenceComponentOnWebexParticipantsListSearchResult(object sender, GenericEventArgs<IEnumerable<WebexParticipantInfo>> args)
		{
			if (args.Data.Any(info => info.ParticipantId == m_Info.ParticipantId))
				UpdateInfo(args.Data.First(info => info.ParticipantId == m_Info.ParticipantId));
		}

		private void ConferenceComponentOnWebexParticipantListUpdated(object sender, GenericEventArgs<WebexParticipantInfo> args)
		{
			if (args.Data.CallId != m_CallId || args.Data.ParticipantId != m_Info.ParticipantId)
				return;

			UpdateInfo(args.Data);
		}

		#endregion
	}
}