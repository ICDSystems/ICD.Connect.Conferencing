﻿using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Conference;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Dialing;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.System;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Video;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.IncomingCalls;
using ICD.Connect.Conferencing.Participants.Enums;
using ICD.Connect.Conferencing.Utils;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Controls.Conference
{
	public sealed class CiscoCodecConferenceControl : AbstractConferenceDeviceControl<CiscoCodecDevice, ICiscoConference>
	{
		#region Events

		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallAdded;
		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallRemoved;
		public override event EventHandler<ConferenceEventArgs> OnConferenceAdded;
		public override event EventHandler<ConferenceEventArgs> OnConferenceRemoved;

		#endregion

		#region Members

		private readonly DialingComponent m_DialingComponent;
		private readonly ConferenceComponent m_ConferenceComponent;
		private readonly SystemComponent m_SystemComponent;
		private readonly VideoComponent m_VideoComponent;

		private readonly BiDictionary<int, TraditionalIncomingCall> m_IncomingCallsByCallId;
		private readonly Dictionary<int, CiscoConference> m_SipConferencesByCallId;
		private readonly Dictionary<int, CiscoWebexConference> m_WebexConferencesByCallId;

		private readonly SafeCriticalSection m_CriticalSection;

		#endregion

		#region Properties

		/// <summary>
		/// Gets the type of conference this dialer supports.
		/// </summary>
		public override eCallType Supports { get { return eCallType.Video | eCallType.Audio; } }

		#endregion

		#region Constructor

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public CiscoCodecConferenceControl(CiscoCodecDevice parent, int id)
			: base(parent, id)
		{
			m_DialingComponent = Parent.Components.GetComponent<DialingComponent>();
			m_ConferenceComponent = Parent.Components.GetComponent<ConferenceComponent>();
			m_SystemComponent = Parent.Components.GetComponent<SystemComponent>();
			m_VideoComponent = Parent.Components.GetComponent<VideoComponent>();

			m_IncomingCallsByCallId = new BiDictionary<int, TraditionalIncomingCall>();
			m_SipConferencesByCallId = new Dictionary<int, CiscoConference>();
			m_WebexConferencesByCallId = new Dictionary<int, CiscoWebexConference>();
			m_CriticalSection = new SafeCriticalSection();

			SupportedConferenceControlFeatures =
				eConferenceControlFeatures.AutoAnswer |
				eConferenceControlFeatures.DoNotDisturb |
				eConferenceControlFeatures.PrivacyMute |
				eConferenceControlFeatures.CanDial |
				eConferenceControlFeatures.Dtmf |
				eConferenceControlFeatures.CameraMute |
				eConferenceControlFeatures.CanEnd;

			Subscribe(m_DialingComponent);
			Subscribe(m_SystemComponent);
			Subscribe(m_VideoComponent);

			UpdatePrivacyMute();
			UpdateDoNotDisturb();
			UpdateAutoAnswer();
			UpdateCameraMute();
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnIncomingCallAdded = null;
			OnIncomingCallRemoved = null;

			base.DisposeFinal(disposing);

			Unsubscribe(m_DialingComponent);
			Unsubscribe(m_SystemComponent);
			Unsubscribe(m_VideoComponent);
		}

		#endregion

		#region Methods

		public override IEnumerable<ICiscoConference> GetConferences()
		{
			List<ICiscoConference> conferences = new List<ICiscoConference>();

			m_CriticalSection.Enter();

			try
			{
				conferences.AddRange(m_SipConferencesByCallId.Values.Cast<ICiscoConference>());
				conferences.AddRange(m_WebexConferencesByCallId.Values.Cast<ICiscoConference>());
			}
			finally
			{
				m_CriticalSection.Leave();
			}

			return conferences;
		}

		/// <summary>
		/// Returns the level of support the dialer has for the given booking.
		/// </summary>
		/// <param name="dialContext"></param>
		/// <returns></returns>
		public override eDialContextSupport CanDial(IDialContext dialContext)
		{
			if(string.IsNullOrEmpty(dialContext.DialString))
				return eDialContextSupport.Unsupported;

			if (dialContext.Protocol == eDialProtocol.Sip && SipUtils.IsValidSipUri(dialContext.DialString))
				return eDialContextSupport.Supported;

			if (dialContext.Protocol == eDialProtocol.Pstn)
				return eDialContextSupport.Supported;

			if (dialContext.Protocol == eDialProtocol.Spark)
				return eDialContextSupport.Supported;

			if (dialContext.Protocol == eDialProtocol.Unknown)
				return eDialContextSupport.Unknown;

			return eDialContextSupport.Unsupported;
		}

		/// <summary>
		/// Dials the given booking.
		/// </summary>
		/// <param name="dialContext"></param>
		public override void Dial(IDialContext dialContext)
		{
			if (dialContext.Protocol == eDialProtocol.Spark)
				m_ConferenceComponent.WebexJoin(dialContext.DialString);
			else
				m_DialingComponent.Dial(dialContext.DialString, dialContext.CallType);
		}

		/// <summary>
		/// Sets the do-not-disturb enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public override void SetDoNotDisturb(bool enabled)
		{
			m_DialingComponent.SetDoNotDisturb(enabled);
		}

		/// <summary>
		/// Sets the auto-answer enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public override void SetAutoAnswer(bool enabled)
		{
			m_DialingComponent.SetAutoAnswer(enabled);
		}

		/// <summary>
		/// Sets the privacy mute enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public override void SetPrivacyMute(bool enabled)
		{
			m_DialingComponent.SetPrivacyMute(enabled);
		}

		public override void SetCameraMute(bool mute)
		{
			m_VideoComponent.SetMainVideoMute(mute);
		}

		public override void StartPersonalMeeting()
		{
			throw new NotSupportedException();
		}

		public override void EnableCallLock(bool enabled)
		{
			throw new NotSupportedException();
		}

		#endregion

		#region Private Methods

		private void UpdatePrivacyMute()
		{
			PrivacyMuted = m_DialingComponent.PrivacyMuted;
		}

		private void UpdateDoNotDisturb()
		{
			DoNotDisturb = m_DialingComponent.DoNotDisturb;
		}

		private void UpdateAutoAnswer()
		{
			AutoAnswer = m_DialingComponent.AutoAnswer;
		}

		private void UpdateCameraMute()
		{
			CameraMute = m_VideoComponent.MainVideoMuted;
		}

		#endregion

		#region Dialing Component Callbacks

		private void Subscribe(DialingComponent component)
		{
			component.OnSourceAdded += ComponentOnSourceAdded;
			component.OnSourceUpdated += ComponentOnSourceUpdated;
			component.OnSourceRemoved += ComponentOnSourceRemoved;
			component.OnPrivacyMuteChanged += ComponentOnPrivacyMuteChanged;
			component.OnAutoAnswerChanged += ComponentOnAutoAnswerChanged;
			component.OnDoNotDisturbChanged += ComponentOnDoNotDisturbChanged;
		}

		private void Unsubscribe(DialingComponent component)
		{
			component.OnSourceAdded -= ComponentOnSourceAdded;
			component.OnSourceUpdated -= ComponentOnSourceUpdated;
			component.OnSourceRemoved -= ComponentOnSourceRemoved;
			component.OnPrivacyMuteChanged -= ComponentOnPrivacyMuteChanged;
			component.OnAutoAnswerChanged -= ComponentOnAutoAnswerChanged;
			component.OnDoNotDisturbChanged -= ComponentOnDoNotDisturbChanged;
		}

		private void ComponentOnSourceAdded(object sender, GenericEventArgs<CallStatus> args)
		{
			CallStatus source = args.Data;

			// If a call is incoming and unanswered, add it as an incoming call
			if (source.Direction == eCallDirection.Incoming && source.AnswerState == eCallAnswerState.Unanswered)
			{
				AddIncomingCall(source);
				return;
			}

			// Add anything else as a regular conference
			AddConference(source);
		}

		private void ComponentOnSourceUpdated(object sender, GenericEventArgs<CallStatus> args)
		{
			CallStatus source = args.Data;

			m_CriticalSection.Enter();

			try
			{
				CiscoConference conference;
				CiscoWebexConference webex;
				
				if (m_SipConferencesByCallId.TryGetValue(source.CallId, out conference))
				{
					conference.UpdateCallStatus(source);
				}
				else if (m_WebexConferencesByCallId.TryGetValue(source.CallId, out webex))
				{
					webex.UpdateCallStatus(source);
				}
			}
			finally
			{
				m_CriticalSection.Leave();
			}
		}

		private void ComponentOnSourceRemoved(object sender, GenericEventArgs<CallStatus> args)
		{
			CallStatus source = args.Data;

			RemoveIncomingCall(source);
			RemoveConference(source);
		}

		private void ComponentOnPrivacyMuteChanged(object sender, BoolEventArgs args)
		{
			UpdatePrivacyMute();
		}

		private void ComponentOnDoNotDisturbChanged(object sender, BoolEventArgs args)
		{
			UpdateDoNotDisturb();
		}

		private void ComponentOnAutoAnswerChanged(object sender, BoolEventArgs args)
		{
			UpdateAutoAnswer();
		}

		#endregion

		#region Conferences

		private void AddConference(CallStatus source)
		{
			ICiscoConference conference = null;
			bool added = false;
			m_CriticalSection.Enter();

			try
			{
				switch (source.Protocol)
				{
					case eCiscoDialProtocol.Unknown:
						break;

					case eCiscoDialProtocol.Spark:
						CiscoWebexConference webexConference;
						if (m_WebexConferencesByCallId.TryGetValue(source.CallId, out webexConference))
						{
							webexConference.UpdateCallStatus(source);
							return;
						}

						webexConference = new CiscoWebexConference(m_ConferenceComponent, m_DialingComponent, source);
						conference = webexConference;
						m_WebexConferencesByCallId.Add(source.CallId, webexConference);
						added = true;
						break;

					case eCiscoDialProtocol.H320:
					case eCiscoDialProtocol.H323:
					case eCiscoDialProtocol.Sip:
						CiscoConference sipConference;
						if (m_SipConferencesByCallId.TryGetValue(source.CallId, out sipConference))
						{
							sipConference.UpdateCallStatus(source);
							return;
						}

						sipConference = new CiscoConference(m_DialingComponent, source);
						conference = sipConference;
						m_SipConferencesByCallId.Add(source.CallId, sipConference);
						added = true;
						break;
				}
			}
			finally
			{
				m_CriticalSection.Leave();
			}

			if (!added)
				return;

			OnConferenceAdded.Raise(this, conference);

			// Initialize sip conference participants after event raise.
			CiscoConference conf = conference as CiscoConference;
			if (conf != null)
				conf.InitializeConference();
		}

		private void RemoveConference(CallStatus source)
		{
			ICiscoConference conference = null;
			bool removed = false;

			m_CriticalSection.Enter();

			try
			{
				switch (source.Protocol)
				{
					case eCiscoDialProtocol.Unknown:
						break;

					case eCiscoDialProtocol.Spark:
						CiscoWebexConference webexConference;
						if (m_WebexConferencesByCallId.Remove(source.CallId, out webexConference))
						{
							removed = true;
							conference = webexConference;
						}
						break;

					case eCiscoDialProtocol.H320:
					case eCiscoDialProtocol.H323:
					case eCiscoDialProtocol.Sip:
						CiscoConference sipConference;
						if (m_SipConferencesByCallId.Remove(source.CallId, out sipConference))
						{
							removed = true;
							conference = sipConference;
						}
						break;
				}
			}
			finally
			{
				m_CriticalSection.Leave();
			}

			if (conference != null)
				conference.Dispose();

			if (removed)
				OnConferenceRemoved.Raise(this, conference);
		}

		#endregion

		#region Incoming Calls

		private void AddIncomingCall(CallStatus source)
		{
			TraditionalIncomingCall incoming;

			m_CriticalSection.Enter();

			try
			{
				if (m_IncomingCallsByCallId.ContainsKey(source.CallId))
					return;

				incoming = new TraditionalIncomingCall(source.CiscoCallType.ToCallType())
				{
					Name = source.Name, 
					Number = source.Number ?? source.RemoteNumber
				};
				m_IncomingCallsByCallId.Add(source.CallId, incoming);

				Subscribe(source);
				Subscribe(incoming);
			}
			finally
			{
				m_CriticalSection.Leave();
			}

			OnIncomingCallAdded.Raise(this, new GenericEventArgs<IIncomingCall>(incoming));
		}

		private void RemoveIncomingCall(CallStatus source)
		{
			TraditionalIncomingCall incoming;

			m_CriticalSection.Enter();

			try
			{
				if (!m_IncomingCallsByCallId.Remove(source.CallId, out incoming))
					return;

				Unsubscribe(source);
				Unsubscribe(incoming);
			}
			finally
			{
				m_CriticalSection.Leave();
			}

			OnIncomingCallRemoved.Raise(this, new GenericEventArgs<IIncomingCall>(incoming));
		}

		#region Incoming Call Callbacks

		private void Subscribe(TraditionalIncomingCall incomingCall)
		{
			incomingCall.AnswerCallback += AnswerCallback;
			incomingCall.RejectCallback += RejectCallback;
		}

		private void Unsubscribe(TraditionalIncomingCall incomingCall)
		{
			incomingCall.AnswerCallback = null;
			incomingCall.RejectCallback = null;
		}

		private void RejectCallback(IIncomingCall sender)
		{
			TraditionalIncomingCall castCall = sender as TraditionalIncomingCall;
			if (castCall == null)
				return;

			int callId = m_CriticalSection.Execute(() => m_IncomingCallsByCallId.GetKey(castCall));
			m_DialingComponent.Reject(callId);
		}

		private void AnswerCallback(IIncomingCall sender)
		{
			TraditionalIncomingCall castCall = sender as TraditionalIncomingCall;
			if (castCall == null)
				return;

			int callId = m_CriticalSection.Execute(() => m_IncomingCallsByCallId.GetKey(castCall));
			m_DialingComponent.Answer(callId);
		}

		#endregion

		#region Source Callbacks

		private void Subscribe(CallStatus source)
		{
			source.OnNameChanged += SourceOnNameChanged;
			source.OnNumberChanged += SourceOnNumberChanged;
			source.OnAnswerStateChanged += SourceOnAnswerStateChanged;
		}

		private void Unsubscribe(CallStatus source)
		{
			source.OnNameChanged -= SourceOnNameChanged;
			source.OnNumberChanged -= SourceOnNumberChanged;
			source.OnAnswerStateChanged -= SourceOnAnswerStateChanged;
		}

		private void SourceOnAnswerStateChanged(object sender, GenericEventArgs<eCallAnswerState> args)
		{
			UpdateIncomingCall(sender as CallStatus);
		}

		private void SourceOnNumberChanged(object sender, StringEventArgs args)
		{
			UpdateIncomingCall(sender as CallStatus);
		}

		private void SourceOnNameChanged(object sender, StringEventArgs args)
		{
			UpdateIncomingCall(sender as CallStatus);
		}

		private void UpdateIncomingCall(CallStatus source)
		{
			TraditionalIncomingCall incomingCall;

			m_CriticalSection.Enter();
			try
			{
				if (!m_IncomingCallsByCallId.TryGetValue(source.CallId, out incomingCall))
					return;
			}
			finally
			{
				m_CriticalSection.Leave();
			}


			incomingCall.Name = source.Name;
			incomingCall.Number = source.Number;
			incomingCall.AnswerState = source.AnswerState;

			switch (incomingCall.AnswerState)
			{
				case eCallAnswerState.Ignored:
				case eCallAnswerState.AutoAnswered:
				case eCallAnswerState.Answered:
					AddConference(source);
					RemoveIncomingCall(source);
					break;
			}
		}

		#endregion

		#endregion

		#region Parent Callbacks

		protected override void Subscribe(CiscoCodecDevice parent)
		{
			base.Subscribe(parent);

			parent.OnConnectedStateChanged += ParentOnConnectedStateChanged;
		}

		protected override void Unsubscribe(CiscoCodecDevice parent)
		{
			base.Unsubscribe(parent);

			parent.OnConnectedStateChanged -= ParentOnConnectedStateChanged;
		}

		private void ParentOnConnectedStateChanged(object sender, BoolEventArgs args)
		{
			if (!args.Data)
				return;

			// If we connected query for any active calls
			Parent.SendCommand("xStatus Call");
			Parent.SendCommand("xCommand Conference ParticipantList Search");
		}

		#endregion

		#region System Component Callbacks

		private void Subscribe(SystemComponent component)
		{
			component.OnSipRegistrationAdded += ComponentOnSipRegistrationAdded;
		}

		private void Unsubscribe(SystemComponent component)
		{
			component.OnSipRegistrationAdded -= ComponentOnSipRegistrationAdded;
		}

		private void ComponentOnSipRegistrationAdded(object sender, IntEventArgs args)
		{
			SipRegistration first = m_SystemComponent.GetSipRegistrations().FirstOrDefault();

			CallInInfo =
				first == null
					? null
					: new DialContext
					{
						Protocol = eDialProtocol.Sip,
						CallType = eCallType.Audio | eCallType.Video,
						DialString = first.Uri
					};
		}

		#endregion

		#region Video Component Callbacks

		private void Subscribe(VideoComponent videoComponent)
		{
			videoComponent.OnMainVideoMutedChanged += VideoComponentOnMainVideoMutedChanged;
		}

		private void Unsubscribe(VideoComponent videoComponent)
		{
			videoComponent.OnMainVideoMutedChanged -= VideoComponentOnMainVideoMutedChanged;
		}

		private void VideoComponentOnMainVideoMutedChanged(object sender, BoolEventArgs e)
		{
			UpdateCameraMute();
		}

		#endregion
	}
}
