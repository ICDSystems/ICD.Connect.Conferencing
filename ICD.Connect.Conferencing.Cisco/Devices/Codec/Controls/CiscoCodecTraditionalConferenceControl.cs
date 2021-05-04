using System;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Dialing;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.System;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Video;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.IncomingCalls;
using ICD.Connect.Conferencing.Participants.Enums;
using ICD.Connect.Conferencing.Utils;
using eCallType = ICD.Connect.Conferencing.EventArguments.eCallType;
using eDialProtocol = ICD.Connect.Conferencing.DialContexts.eDialProtocol;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Controls
{
	public sealed class CiscoCodecTraditionalConferenceControl : AbstractTraditionalConferenceDeviceControl<CiscoCodecDevice>
	{
		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallAdded;
		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallRemoved;
		public event EventHandler<BoolEventArgs> OnSipEnabledChanged;
		public event EventHandler<StringEventArgs> OnSipLocalNameChanged;
		public event EventHandler<StringEventArgs> OnSipRegistrationStatusChanged;

		private readonly DialingComponent m_DialingComponent;
		private readonly SystemComponent m_SystemComponent;
		private readonly VideoComponent m_VideoComponent;

		private readonly IcdHashSet<SipRegistration> m_SubscribedRegistrations;
		private readonly BiDictionary<CallComponent, TraditionalIncomingCall> m_IncomingCalls;

		private readonly SafeCriticalSection m_CriticalSection;

		#region Properties

		/// <summary>
		/// Gets the type of conference this dialer supports.
		/// </summary>
		public override eCallType Supports { get { return eCallType.Video | eCallType.Audio; } }

		public bool SipIsRegistered
		{
			get
			{
				var registrations = m_SystemComponent.GetSipRegistrations().ToIcdHashSet();
				return registrations.Any(r => r.Registration == eRegState.Registered)
					&& registrations.All(r => r.Registration != eRegState.Failed);
			}
		}

		public string SipLocalName
		{
			get
			{
				return string.Join(", ", m_SystemComponent.GetSipRegistrations()
														  .Select(r => r.Uri)
														  .ToArray());
			}
		}

		public string SipRegistrationStatus
		{
			get
			{
				return string.Join(", ", m_SystemComponent.GetSipRegistrations()
														  .Select(r => r.Registration
																		.ToString())
														  .ToArray());
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public CiscoCodecTraditionalConferenceControl(CiscoCodecDevice parent, int id)
			: base(parent, id)
		{
			m_DialingComponent = Parent.Components.GetComponent<DialingComponent>();
			m_SystemComponent = Parent.Components.GetComponent<SystemComponent>();
			m_VideoComponent = Parent.Components.GetComponent<VideoComponent>();

			m_SubscribedRegistrations = new IcdHashSet<SipRegistration>();
			m_IncomingCalls = new BiDictionary<CallComponent, TraditionalIncomingCall>();
			m_CriticalSection = new SafeCriticalSection();

			SupportedConferenceControlFeatures =
				eConferenceControlFeatures.AutoAnswer |
				eConferenceControlFeatures.DoNotDisturb |
				eConferenceControlFeatures.PrivacyMute |
				eConferenceControlFeatures.CanDial |
				eConferenceControlFeatures.Dtmf |
				eConferenceControlFeatures.Hold |
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
			OnSipEnabledChanged = null;
			OnSipLocalNameChanged = null;
			OnSipRegistrationStatusChanged = null;

			base.DisposeFinal(disposing);

			Unsubscribe(m_DialingComponent);
			Unsubscribe(m_SystemComponent);
			Unsubscribe(m_VideoComponent);
		}

		#region Methods

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
			component.OnSourceRemoved += ComponentOnSourceRemoved;
			component.OnPrivacyMuteChanged += ComponentOnPrivacyMuteChanged;
			component.OnAutoAnswerChanged += ComponentOnAutoAnswerChanged;
			component.OnDoNotDisturbChanged += ComponentOnDoNotDisturbChanged;
		}

		private void Unsubscribe(DialingComponent component)
		{
			component.OnSourceAdded -= ComponentOnSourceAdded;
			component.OnSourceRemoved -= ComponentOnSourceRemoved;
			component.OnPrivacyMuteChanged -= ComponentOnPrivacyMuteChanged;
			component.OnAutoAnswerChanged -= ComponentOnAutoAnswerChanged;
			component.OnDoNotDisturbChanged -= ComponentOnDoNotDisturbChanged;
		}

		private void ComponentOnSourceAdded(object sender, GenericEventArgs<CallComponent> args)
		{
			CallComponent source = args.Data;

			switch (source.Direction)
			{
				case eCallDirection.Undefined:
					break;

				case eCallDirection.Incoming:
					//AddParticipant(source);
					if (source.AnswerState == eCallAnswerState.Unanswered)
						AddIncomingCall(source);
					break;
				case eCallDirection.Outgoing:
					AddParticipant(source);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void ComponentOnSourceRemoved(object sender, GenericEventArgs<CallComponent> args)
		{
			CallComponent source = args.Data;

			RemoveIncomingCall(source);
			RemoveParticipant(source);
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

		#region Incoming Calls

		private void AddIncomingCall(CallComponent source)
		{
			TraditionalIncomingCall incoming;

			m_CriticalSection.Enter();

			try
			{
				if (m_IncomingCalls.ContainsKey(source))
					return;

				incoming = new TraditionalIncomingCall(source.CallType)
				{
					Name = source.Name, 
					Number = source.Number ?? source.RemoteNumber
				};
				m_IncomingCalls.Add(source, incoming);

				Subscribe(source);
				Subscribe(incoming);
			}
			finally
			{
				m_CriticalSection.Leave();
			}

			OnIncomingCallAdded.Raise(this, new GenericEventArgs<IIncomingCall>(incoming));
		}

		private void RemoveIncomingCall(CallComponent source)
		{
			TraditionalIncomingCall incoming;

			m_CriticalSection.Enter();

			try
			{
				if (!m_IncomingCalls.TryGetValue(source, out incoming))
					return;

				Unsubscribe(source);
				Unsubscribe(incoming);

				m_IncomingCalls.RemoveKey(source);
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

			CallComponent source = m_CriticalSection.Execute(() => m_IncomingCalls.GetKey(castCall));
			source.Reject();
		}

		private void AnswerCallback(IIncomingCall sender)
		{
			TraditionalIncomingCall castCall = sender as TraditionalIncomingCall;
			if (castCall == null)
				return;

			CallComponent source = m_CriticalSection.Execute(() => m_IncomingCalls.GetKey(castCall));
			source.Answer();
		}

		#endregion

		#region Source Callbacks

		private void Subscribe(CallComponent source)
		{
			source.OnNameChanged += SourceOnNameChanged;
			source.OnNumberChanged += SourceOnNumberChanged;
			source.OnAnswerStateChanged += SourceOnAnswerStateChanged;
		}

		private void Unsubscribe(CallComponent source)
		{
			source.OnNameChanged -= SourceOnNameChanged;
			source.OnNumberChanged -= SourceOnNumberChanged;
			source.OnAnswerStateChanged -= SourceOnAnswerStateChanged;
		}

		private void SourceOnAnswerStateChanged(object sender, CallAnswerStateEventArgs args)
		{
			UpdateIncomingCall(sender as CallComponent);
		}

		private void SourceOnNumberChanged(object sender, StringEventArgs args)
		{
			UpdateIncomingCall(sender as CallComponent);
		}

		private void SourceOnNameChanged(object sender, StringEventArgs args)
		{
			UpdateIncomingCall(sender as CallComponent);
		}

		private void UpdateIncomingCall(CallComponent source)
		{
			TraditionalIncomingCall incomingCall = m_CriticalSection.Execute(() => m_IncomingCalls.GetValue(source));

			incomingCall.Name = source.Name;
			incomingCall.Number = source.Number;
			incomingCall.AnswerState = source.AnswerState;

			switch (incomingCall.AnswerState)
			{
				case eCallAnswerState.Ignored:
				case eCallAnswerState.AutoAnswered:
				case eCallAnswerState.Answered:
					AddParticipant(m_IncomingCalls.GetKey(incomingCall));
					RemoveIncomingCall(source);
					break;
			}
		}

		#endregion

		#endregion

		#region System Component Callbacks

		private void Subscribe(SystemComponent component)
		{
			component.OnSipRegistrationAdded += ComponentOnSipRegistrationAdded;
		}

		private void Unsubscribe(SystemComponent component)
		{
			component.OnSipRegistrationAdded -= ComponentOnSipRegistrationAdded;

			foreach (SipRegistration registration in m_SubscribedRegistrations)
				Unsubscribe(registration);
			m_SubscribedRegistrations.Clear();
		}

		private void ComponentOnSipRegistrationAdded(object sender, IntEventArgs args)
		{
			SipRegistration registration = m_SystemComponent.GetSipRegistration(args.Data);
			Subscribe(registration);

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

			OnSipLocalNameChanged.Raise(this, new StringEventArgs(SipLocalName));
			OnSipEnabledChanged.Raise(this, new BoolEventArgs(SipIsRegistered));
			OnSipRegistrationStatusChanged.Raise(this, new StringEventArgs(SipRegistrationStatus));
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

		#region SIP Registration Callbacks

		private void Subscribe(SipRegistration registration)
		{
			registration.OnRegistrationChange += RegistrationOnRegistrationChange;
			registration.OnUriChange += RegistrationOnUriChange;

			m_SubscribedRegistrations.Add(registration);
		}

		private void Unsubscribe(SipRegistration registration)
		{
			registration.OnRegistrationChange -= RegistrationOnRegistrationChange;
			registration.OnUriChange -= RegistrationOnUriChange;
		}

		private void RegistrationOnRegistrationChange(object sender, RegistrationEventArgs registrationEventArgs)
		{
			OnSipRegistrationStatusChanged.Raise(this, new StringEventArgs(SipRegistrationStatus));
			OnSipEnabledChanged.Raise(this, new BoolEventArgs(SipIsRegistered));
		}

		private void RegistrationOnUriChange(object sender, StringEventArgs stringEventArgs)
		{
			OnSipLocalNameChanged.Raise(this, new StringEventArgs(SipLocalName));
		}

		#endregion
	}
}
