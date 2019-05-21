using System;
using System.Linq;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Dialing;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.System;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.Participants;
using ICD.Connect.Conferencing.Utils;
using eCallType = ICD.Connect.Conferencing.EventArguments.eCallType;
using eDialProtocol = ICD.Connect.Conferencing.DialContexts.eDialProtocol;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Controls
{
	public sealed class CiscoCodecTraditionalConferenceControl : AbstractTraditionalConferenceDeviceControl<CiscoCodecDevice>, ISipDialingDeviceControl
	{
		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallAdded;
		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallRemoved;
		public event EventHandler<BoolEventArgs> OnSipEnabledChanged;
		public event EventHandler<StringEventArgs> OnSipLocalNameChanged;
		public event EventHandler<StringEventArgs> OnSipRegistrationStatusChanged;

		private readonly DialingComponent m_DialingComponent;
		private readonly SystemComponent m_SystemComponent;
		private readonly IcdHashSet<SipRegistration> m_SubscribedRegistrations = new IcdHashSet<SipRegistration>(); 

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
			Subscribe(m_DialingComponent);
			Subscribe(m_SystemComponent);

			UpdatePrivacyMute();
			UpdateDoNotDisturb();
			UpdateAutoAnswer();
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

		#endregion

		#region Component Callbacks

		private void Subscribe(DialingComponent component)
		{
			component.OnSourceAdded += ComponentOnParticipantAdded;
			component.OnSourceRemoved += ComponentOnParticipantRemoved;
			component.OnPrivacyMuteChanged += ComponentOnPrivacyMuteChanged;
			component.OnAutoAnswerChanged += ComponentOnAutoAnswerChanged;
			component.OnDoNotDisturbChanged += ComponentOnDoNotDisturbChanged;
		}

		private void Subscribe(SystemComponent component)
		{
			component.OnSipRegistrationAdded += ComponentOnSipRegistrationAdded;
		}

		private void Subscribe(SipRegistration registration)
		{
			registration.OnRegistrationChange += RegistrationOnRegistrationChange;
			registration.OnUriChange += RegistrationOnUriChange;
			m_SubscribedRegistrations.Add(registration);
		}

		private void Unsubscribe(DialingComponent component)
		{
			component.OnSourceAdded -= ComponentOnParticipantAdded;
			component.OnSourceRemoved -= ComponentOnParticipantRemoved;
			component.OnPrivacyMuteChanged -= ComponentOnPrivacyMuteChanged;
			component.OnAutoAnswerChanged -= ComponentOnAutoAnswerChanged;
			component.OnDoNotDisturbChanged -= ComponentOnDoNotDisturbChanged;
		}

		private void Unsubscribe(SystemComponent component)
		{
			component.OnSipRegistrationAdded -= ComponentOnSipRegistrationAdded;
			foreach (var registration in m_SubscribedRegistrations)
				Unsubscribe(registration);

			m_SubscribedRegistrations.Clear();
		}

		private void Unsubscribe(SipRegistration registration)
		{
			registration.OnRegistrationChange -= RegistrationOnRegistrationChange;
			registration.OnUriChange -= RegistrationOnUriChange;
		}

		private void ComponentOnParticipantAdded(object sender, GenericEventArgs<ITraditionalParticipant> args)
		{
			AddParticipant(args.Data);
		}

		private void ComponentOnParticipantRemoved(object sender, GenericEventArgs<ITraditionalParticipant> args)
		{
			RemoveParticipant(args.Data);
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

		private void ComponentOnSipRegistrationAdded(object sender, IntEventArgs args)
		{
			SipRegistration registration = m_SystemComponent.GetSipRegistration(args.Data);
			Subscribe(registration);
			OnSipLocalNameChanged.Raise(this, new StringEventArgs(SipLocalName));
			OnSipEnabledChanged.Raise(this, new BoolEventArgs(SipIsRegistered));
			OnSipRegistrationStatusChanged.Raise(this, new StringEventArgs(SipRegistrationStatus));
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
