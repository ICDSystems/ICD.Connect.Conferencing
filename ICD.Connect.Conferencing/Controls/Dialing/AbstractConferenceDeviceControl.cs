using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Logging.Activities;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Logging.LoggingContexts;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.IncomingCalls;
using ICD.Connect.Conferencing.Participants;
using ICD.Connect.Devices;
using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Conferencing.Controls.Dialing
{
	public abstract class AbstractConferenceDeviceControl<T, TConference> : AbstractDeviceControl<T>, IConferenceDeviceControl<TConference>
		where T : IDevice
		where TConference : IConference
	{
		/// <summary>
		/// Raised when an incoming call is added to the dialing control.
		/// </summary>
		public abstract event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallAdded;

		/// <summary>
		/// Raised when an incoming call is removed from the dialing control.
		/// </summary>
		public abstract event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallRemoved;

		/// <summary>
		/// Raised when a conference is added to the dialing control.
		/// </summary>
		public event EventHandler<ConferenceEventArgs> OnConferenceAdded;

		/// <summary>
		/// Raised when a conference is removed from the dialing control.
		/// </summary>
		public event EventHandler<ConferenceEventArgs> OnConferenceRemoved;

		/// <summary>
		/// Raised when the call-in info for the conference control changes.
		/// </summary>
		public event EventHandler<GenericEventArgs<IDialContext>> OnCallInInfoChanged;

		/// <summary>
		/// Raised when the Do Not Disturb state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnDoNotDisturbChanged;

		/// <summary>
		/// Raised when the Auto Answer state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnAutoAnswerChanged;

		/// <summary>
		/// Raised when the microphones mute state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnPrivacyMuteChanged;

		/// <summary>
		/// Raised when the camera's mute state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnCameraMuteChanged;

		/// <summary>
		/// Raised when the Sip enabled state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnSipEnabledChanged;

		/// <summary>
		/// Raised when the Sip local name changes.
		/// </summary>
		public event EventHandler<StringEventArgs> OnSipLocalNameChanged;

		/// <summary>
		/// Raised when the Sip registration status changes.
		/// </summary>
		public event EventHandler<StringEventArgs> OnSipRegistrationStatusChanged;

		/// <summary>
		/// Raised when the call lock status changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnCallLockChanged;

		/// <summary>
		/// Raised when we start/stop being the host of the active conference.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnAmIHostChanged;

		/// <summary>
		/// Raised when the supported conference features change.
		/// </summary>
		public event EventHandler<ConferenceControlSupportedConferenceFeaturesChangedApiEventArgs> OnSupportedConferenceFeaturesChanged;

		private readonly SafeCriticalSection m_StateSection;

		private bool m_AutoAnswer;
		private bool m_PrivacyMuted;
		private bool m_DoNotDisturb;
		private bool m_CameraMute;
		private bool m_AmIHost;
		private bool m_CallLock;
		private eConferenceControlFeatures m_SupportedConferenceControlFeatures;
		private IDialContext m_CallInInfo;
		private Conference m_ActiveConference;

		#region Properties

		/// <summary>
		/// Gets the type of conference this dialer supports.
		/// </summary>
		public abstract eCallType Supports { get; }

		/// <summary>
		/// Gets the call-in info for this conference control.
		/// </summary>
		public IDialContext CallInInfo
		{
			get { return m_CallInInfo; }
			protected set
			{
				if (value == m_CallInInfo)
					return;

				m_CallInInfo = value;

				Logger.LogSetTo(eSeverity.Informational, "CallInInfo", m_CallInInfo);

				OnCallInInfoChanged.Raise(this, new GenericEventArgs<IDialContext>(m_CallInInfo));
			}
		}

		/// <summary>
		/// Gets the AutoAnswer state.
		/// </summary>
		[PublicAPI]
		public bool AutoAnswer
		{
			get { return m_StateSection.Execute(() => m_AutoAnswer); }
			protected set
			{
				m_StateSection.Enter();

				try
				{
					if (value == m_AutoAnswer)
						return;

					m_AutoAnswer = value;

					Logger.LogSetTo(eSeverity.Informational, "AutoAnswer", m_AutoAnswer);
				}
				finally
				{
					m_StateSection.Leave();
				}

				OnAutoAnswerChanged.Raise(this, new BoolEventArgs(m_AutoAnswer));
			}
		}

		/// <summary>
		/// Gets the current microphone mute state.
		/// </summary>
		[PublicAPI]
		public bool PrivacyMuted
		{
			get { return m_StateSection.Execute(() => m_PrivacyMuted); }
			protected set
			{
				m_StateSection.Enter();

				try
				{
					if (value == m_PrivacyMuted)
						return;

					m_PrivacyMuted = value;

					Logger.LogSetTo(eSeverity.Informational, "PrivacyMuted", m_PrivacyMuted);
				}
				finally
				{
					m_StateSection.Leave();

					Activities.LogActivity(ConferenceDeviceControlActivities.GetPrivacyMuteActivity(m_PrivacyMuted));
				}

				OnPrivacyMuteChanged.Raise(this, new BoolEventArgs(m_PrivacyMuted));
			}
		}

		/// <summary>
		/// Gets the DoNotDisturb state.
		/// </summary>
		[PublicAPI]
		public bool DoNotDisturb
		{
			get { return m_StateSection.Execute(() => m_DoNotDisturb); }
			protected set
			{
				m_StateSection.Enter();

				try
				{
					if (value == m_DoNotDisturb)
						return;

					m_DoNotDisturb = value;

					Logger.LogSetTo(eSeverity.Informational, "DoNotDisturb", m_DoNotDisturb);
				}
				finally
				{
					m_StateSection.Leave();
				}

				OnDoNotDisturbChanged.Raise(this, new BoolEventArgs(m_DoNotDisturb));
			}
		}

		/// <summary>
		/// Gets the camera mute state.
		/// </summary>
		[PublicAPI]
		public bool CameraMute
		{
			get { return m_CameraMute; }
			protected set
			{
				m_StateSection.Enter();

				try
				{
					if (m_CameraMute == value)
						return;

					m_CameraMute = value;

					Logger.LogSetTo(eSeverity.Informational, "Camera Mute", m_CameraMute);
				}
				finally
				{
					m_StateSection.Leave();
				}

				OnCameraMuteChanged.Raise(this, new BoolEventArgs(value));
			}
		}

		// TODO should these be virtual?
		public virtual bool SipIsRegistered { get; }
		public virtual string SipLocalName { get; }
		public virtual string SipRegistrationStatus { get; }

		/// <summary>
		/// Returns true if we are the host of the current conference.
		/// </summary>
		public bool AmIHost
		{
			get { return m_AmIHost; }
			protected set
			{
				m_StateSection.Enter();

				try
				{
					if (value == m_AmIHost)
						return;

					m_AmIHost = value;

					Logger.LogSetTo(eSeverity.Informational, "AmIHost", m_AmIHost);
				}
				finally
				{
					m_StateSection.Leave();
				}

				OnAmIHostChanged.Raise(this, new BoolEventArgs(m_AmIHost));
			}
		}

		/// <summary>
		/// Gets the CallLock State.
		/// </summary>
		public bool CallLock
		{
			get { return m_CallLock; }
			protected set
			{
				m_StateSection.Enter();

				try
				{
					if (value == m_CallLock)
						return;

					m_CallLock = value;

					Logger.LogSetTo(eSeverity.Informational, "CallLock", eSeverity.Informational);
				}
				finally
				{
					m_StateSection.Leave();

					Activities.LogActivity(m_CallLock
						                       ? new Activity(Activity.ePriority.Medium, "Call Lock", "Call Lock Enabled", eSeverity.Informational)
						                       : new Activity(Activity.ePriority.Low, "Call Lock", "Call Lock Disabled", eSeverity.Informational));
				}

				OnCallLockChanged.Raise(this, new BoolEventArgs(m_CallLock));
			}
		}

		/// <summary>
		/// Returns the features that are supported by this conference control.
		/// </summary>
		public eConferenceControlFeatures SupportedConferenceControlFeatures
		{
			get { return m_SupportedConferenceControlFeatures; }
			protected set
			{
				if (value == m_SupportedConferenceControlFeatures)
					return;

				m_SupportedConferenceControlFeatures = value;

				OnSupportedConferenceFeaturesChanged.Raise(this,
				                                           new ConferenceControlSupportedConferenceFeaturesChangedApiEventArgs(
					                                           m_SupportedConferenceControlFeatures));
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		protected AbstractConferenceDeviceControl(T parent, int id)
			: base(parent, id)
		{
			m_StateSection = new SafeCriticalSection();

			// Initialize activities
			PrivacyMuted = false;
			CallLock = false;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		/// <param name="uuid"></param>
		protected AbstractConferenceDeviceControl(T parent, int id, Guid uuid)
			: base(parent, id, uuid)
		{
			m_StateSection = new SafeCriticalSection();

			// Initialize activities
			PrivacyMuted = false;
			CallLock = false;
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnConferenceAdded = null;
			OnConferenceRemoved = null;
			OnDoNotDisturbChanged = null;
			OnAutoAnswerChanged = null;
			OnPrivacyMuteChanged = null;
			OnSupportedConferenceFeaturesChanged = null;
			OnCallInInfoChanged = null;
			OnCameraMuteChanged = null;
			OnSipEnabledChanged = null;
			OnSipLocalNameChanged = null;
			OnSipRegistrationStatusChanged = null;
			OnSupportedConferenceFeaturesChanged = null;
			OnCallLockChanged = null;
			OnAmIHostChanged = null;

			base.DisposeFinal(disposing);
		}

		#region Methods

		/// <summary>
		/// Gets the active conference sources.
		/// </summary>
		/// <returns></returns>
		IEnumerable<IConference> IConferenceDeviceControl.GetConferences()
		{
			if (m_ActiveConference != null)
				yield return m_ActiveConference;

			foreach (IConference conference in GetConferences().Cast<IConference>())
				yield return conference;
		}

		/// <summary>
		/// Gets the active conference sources.
		/// </summary>
		/// <returns></returns>
		public abstract IEnumerable<TConference> GetConferences();

		/// <summary>
		/// Returns the level of support the device has for the given context.
		/// </summary>
		/// <param name="dialContext"></param>
		/// <returns></returns>
		public abstract eDialContextSupport CanDial(IDialContext dialContext);

		/// <summary>
		/// Dials the given context.
		/// </summary>
		/// <param name="dialContext"></param>
		public abstract void Dial(IDialContext dialContext);

		/// <summary>
		/// Sets the do-not-disturb enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public abstract void SetDoNotDisturb(bool enabled);

		/// <summary>
		/// Sets the auto-answer enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public abstract void SetAutoAnswer(bool enabled);

		/// <summary>
		/// Sets the privacy mute enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public abstract void SetPrivacyMute(bool enabled);

		/// <summary>
		/// Sets the camera mute state.
		/// </summary>
		/// <param name="mute"></param>
		public abstract void SetCameraMute(bool mute);

		/// <summary>
		/// Starts a personal meeting.
		/// </summary>
		public abstract void StartPersonalMeeting();

		/// <summary>
		/// Locks the current active conference so no more participants may join.
		/// </summary>
		/// <param name="enabled"></param>
		public abstract void EnableCallLock(bool enabled);

		#endregion

		#region Conference Events

		/// <summary>
		/// Allows for child implementations to safely raise the OnConferenceAdded event.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected void RaiseOnConferenceAdded(object sender, ConferenceEventArgs args)
		{
			OnConferenceAdded.Raise(sender, args);
		}

		/// <summary>
		/// Allows for child implementations to safely raise the OnConferenceRemoved event.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected void RaiseOnConferenceRemoved(object sender, ConferenceEventArgs args)
		{
			OnConferenceRemoved.Raise(sender, args);
		}

		#endregion

		#region Sip Events

		/// <summary>
		/// Allows for child implementations to safely raise the OnSipEnabledChanged event.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected void RaiseSipEnabledState(object sender, BoolEventArgs args)
		{
			OnSipEnabledChanged.Raise(sender, args);
		}

		/// <summary>
		/// Allows for child implementations to safely raise the OnSipLocalNameChanged event.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected void RaiseSipLocalName(object sender, StringEventArgs args)
		{
			OnSipLocalNameChanged.Raise(sender, args);
		}

		/// <summary>
		/// Allows for child implementations to safely raise the OnSipRegistrationStatusChanged event.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected void RaiseSipRegistrationStatus(object sender, StringEventArgs args)
		{
			OnSipRegistrationStatusChanged.Raise(sender, args);
		}

		#endregion

		#region Source Events

		protected void AddParticipant(IParticipant participant)
		{
			if (m_ActiveConference == null)
			{
				m_ActiveConference = new Conference();
				OnConferenceAdded.Raise(this, new ConferenceEventArgs(m_ActiveConference));
			}

			m_ActiveConference.AddParticipant(participant);
		}

		protected void RemoveParticipant(IParticipant participant)
		{
			if (m_ActiveConference == null)
				return;

			m_ActiveConference.RemoveParticipant(participant);

			if (!m_ActiveConference.GetParticipants().Any())
			{
				OnConferenceRemoved.Raise(this, new ConferenceEventArgs(m_ActiveConference));
				m_ActiveConference = null;
			}
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			foreach (IConsoleCommand command in ConferenceDeviceControlConsole.GetConsoleCommands(this))
				yield return command;
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			ConferenceDeviceControlConsole.BuildConsoleStatus(this, addRow);
		}

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			foreach (IConsoleNodeBase node in ConferenceDeviceControlConsole.GetConsoleNodes(this))
				yield return node;
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
		{
			return base.GetConsoleNodes();
		}

		#endregion
	}
}
