using System;
using System.Collections.Generic;
using System.Linq;
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
		private readonly ConferenceComponent m_ConferenceComponent;
		private readonly int m_CallId;		
		private readonly string m_WebexParticipantId;

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
		/// <param name="callId"></param>
		/// <param name="conferenceComponent"></param>
		public CiscoWebexParticipant(WebexParticipantInfo info, int callId, [NotNull] ConferenceComponent conferenceComponent)
		{
			if (conferenceComponent == null)
				throw new ArgumentNullException("conferenceComponent");

			m_ConferenceComponent = conferenceComponent;
			m_CallId = callId;
			m_WebexParticipantId = info.ParticipantId;
			IsSelf = info.IsSelf;
			UpdateInfo(info);

			
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
			m_ConferenceComponent.ParticipantAdmit(m_CallId, WebexParticipantId);
		}

		public override void Kick()
		{
			m_ConferenceComponent.ParticipantDisconnect(m_CallId, WebexParticipantId);
		}

		public override void Mute(bool mute)
		{
			m_ConferenceComponent.ParticipantMute(mute, m_CallId, WebexParticipantId);
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

		private void UpdateInfo(WebexParticipantInfo info)
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
			if (args.Data.Any(info => info.ParticipantId == WebexParticipantId))
				UpdateInfo(args.Data.First(info => info.ParticipantId == WebexParticipantId));
		}

		private void ConferenceComponentOnWebexParticipantListUpdated(object sender, GenericEventArgs<WebexParticipantInfo> args)
		{
			if (args.Data.CallId != m_CallId || args.Data.ParticipantId != WebexParticipantId)
				return;

			UpdateInfo(args.Data);
		}

		#endregion
	}
}