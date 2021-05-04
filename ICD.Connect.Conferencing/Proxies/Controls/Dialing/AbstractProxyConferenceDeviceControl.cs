using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.IncomingCalls;
using ICD.Connect.Devices.Proxies.Controls;
using ICD.Connect.Devices.Proxies.Devices;

namespace ICD.Connect.Conferencing.Proxies.Controls.Dialing
{
	public abstract class AbstractProxyConferenceDeviceControl<T> : AbstractProxyDeviceControl, IProxyConferenceDeviceControl<T>
		where T : IConference
	{
		#region Events

		public event EventHandler<ConferenceEventArgs> OnConferenceAdded;
		public event EventHandler<ConferenceEventArgs> OnConferenceRemoved;
		public event EventHandler<GenericEventArgs<IDialContext>> OnCallInInfoChanged;
		public event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallAdded;
		public event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallRemoved;
		public event EventHandler<BoolEventArgs> OnDoNotDisturbChanged;
		public event EventHandler<BoolEventArgs> OnAutoAnswerChanged;
		public event EventHandler<BoolEventArgs> OnPrivacyMuteChanged;
		public event EventHandler<BoolEventArgs> OnCameraMuteChanged;
		public event EventHandler<BoolEventArgs> OnSipEnabledChanged;
		public event EventHandler<StringEventArgs> OnSipLocalNameChanged;
		public event EventHandler<StringEventArgs> OnSipRegistrationStatusChanged;
		public event EventHandler<BoolEventArgs> OnCallLockChanged;
		public event EventHandler<BoolEventArgs> OnAmIHostChanged;
		public event EventHandler<ConferenceControlSupportedConferenceFeaturesChangedApiEventArgs> OnSupportedConferenceFeaturesChanged;

		#endregion

		private bool m_AutoAnswer;
		private bool m_PrivacyMuted;
		private bool m_DoNotDisturb;
		private bool m_CameraMute;
		private bool m_SipIsRegistered;
		private string m_SipLocalName;
		private string m_SipRegistrationStatus;
		private bool m_AmIHost;
		private bool m_CallLock;
		private eConferenceControlFeatures m_SupportedConferenceControlFeatures;
		private IDialContext m_CallInInfo;

		#region Properties

		/// <summary>
		/// Gets the call-in info for this conference control.
		/// </summary>
		public IDialContext CallInInfo
		{
			get { return m_CallInInfo; }
			[UsedImplicitly]
			private set
			{
				if (value == m_CallInInfo)
					return;

				m_CallInInfo = value;

				OnCallInInfoChanged.Raise(this, new GenericEventArgs<IDialContext>(m_CallInInfo));
			}
		}

		/// <summary>
		/// Gets the AutoAnswer state.
		/// </summary>
		public bool AutoAnswer
		{
			get { return m_AutoAnswer; }
			[UsedImplicitly]
			private set
			{
				if (value == m_AutoAnswer)
					return;

				m_AutoAnswer = value;

				OnAutoAnswerChanged.Raise(this, new BoolEventArgs(m_AutoAnswer));
			}
		}

		/// <summary>
		/// Gets the current microphone mute state.
		/// </summary>
		public bool PrivacyMuted
		{
			get { return m_PrivacyMuted; }
			[UsedImplicitly]
			private set
			{
				if (value == m_PrivacyMuted)
					return;

				m_PrivacyMuted = value;

				OnPrivacyMuteChanged.Raise(this, new BoolEventArgs(m_PrivacyMuted));
			}
		}

		/// <summary>
		/// Gets the DoNotDisturb state.
		/// </summary>
		public bool DoNotDisturb
		{
			get { return m_DoNotDisturb; }
			[UsedImplicitly]
			private set
			{
				if (value == m_DoNotDisturb)
					return;

				m_DoNotDisturb = value;

				OnDoNotDisturbChanged.Raise(this, new BoolEventArgs(m_DoNotDisturb));
			}
		}

		/// <summary>
		/// Gets the current camera mute state.
		/// </summary>
		public bool CameraMute
		{
			get { return m_CameraMute; }
			[UsedImplicitly]
			private set
			{
				if (value == m_CameraMute)
					return;

				m_CameraMute = value;

				OnCameraMuteChanged.Raise(this, new BoolEventArgs(m_CameraMute));
			}
		}

		public bool SipIsRegistered
		{
			get { return m_SipIsRegistered; }
			[UsedImplicitly]
			private set
			{
				if (value == m_SipIsRegistered)
					return;

				m_SipIsRegistered = value;

				OnSipEnabledChanged.Raise(this, new BoolEventArgs(m_SipIsRegistered));
			}
		}

		public string SipLocalName
		{
			get { return m_SipLocalName; }
			[UsedImplicitly]
			private set
			{
				if (value == m_SipLocalName)
					return;

				m_SipLocalName = value;

				OnSipLocalNameChanged.Raise(this, new StringEventArgs(m_SipLocalName));
			}
		}

		public string SipRegistrationStatus
		{
			get { return m_SipRegistrationStatus; }
			[UsedImplicitly]
			private set
			{
				if (value == m_SipRegistrationStatus)
					return;

				m_SipRegistrationStatus = value;

				OnSipRegistrationStatusChanged.Raise(this, new StringEventArgs(m_SipRegistrationStatus));
			}
		}

		/// <summary>
		/// Returns true if we are the host of the active conference.
		/// </summary>
		public bool AmIHost
		{
			get { return m_AmIHost; }
			[UsedImplicitly]
			private set
			{
				if (value == m_AmIHost)
					return;

				m_AmIHost = value;

				OnAmIHostChanged.Raise(this, new BoolEventArgs(m_AmIHost));
			}
		}

		/// <summary>
		/// Gets the CallLock State.
		/// </summary>
		public bool CallLock
		{
			get { return m_CallLock; }
			[UsedImplicitly]
			private set
			{
				if (value == m_CallLock)
					return;

				m_CallLock = value;

				OnCallLockChanged.Raise(this, new BoolEventArgs(m_CallLock));
			}
		}

		/// <summary>
		/// Returns the features that are supported by this conference control.
		/// </summary>
		public eConferenceControlFeatures SupportedConferenceControlFeatures
		{
			get { return m_SupportedConferenceControlFeatures; }
			[UsedImplicitly]
			private set
			{
				if (value == m_SupportedConferenceControlFeatures)
					return;

				m_SupportedConferenceControlFeatures = value;

				OnSupportedConferenceFeaturesChanged.Raise(this,
				                                           new ConferenceControlSupportedConferenceFeaturesChangedApiEventArgs(
					                                           m_SupportedConferenceControlFeatures));
			}
		}

		/// <summary>
		/// Gets the type of conference this dialer supports.
		/// </summary>
		public eCallType Supports { get; [UsedImplicitly] private set; }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		protected AbstractProxyConferenceDeviceControl(IProxyDevice parent, int id)
			: base(parent, id)
		{
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnConferenceAdded = null;
			OnConferenceRemoved = null;
			OnIncomingCallAdded = null;
			OnIncomingCallRemoved = null;
			OnDoNotDisturbChanged = null;
			OnAutoAnswerChanged = null;
			OnPrivacyMuteChanged = null;
			OnCallInInfoChanged = null;
			OnCameraMuteChanged = null;
			OnSipEnabledChanged = null;
			OnSipLocalNameChanged = null;
			OnSipRegistrationStatusChanged = null;
			OnCallLockChanged = null;
			OnAmIHostChanged = null;

			base.DisposeFinal(disposing);
		}

		#region Methods

		/// <summary>
		/// Gets the active conference sources.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<T> GetConferences()
		{
			// TODO
			yield break;
		}

		IEnumerable<IConference> IConferenceDeviceControl.GetConferences()
		{
			return GetConferences().Cast<IConference>();
		} 

		/// <summary>
		/// Returns the level of support the device has for the given booking.
		/// </summary>
		/// <param name="dialContext"></param>
		/// <returns></returns>
		public eDialContextSupport CanDial(IDialContext dialContext)
		{
			// TODO ???
			return eDialContextSupport.Unknown;
		}

		/// <summary>
		/// Dials the given booking.
		/// </summary>
		/// <param name="dialContext"></param>
		public void Dial(IDialContext dialContext)
		{
			CallMethod(ConferenceDeviceControlApi.METHOD_DIAL_CONTEXT, dialContext);
		}

		/// <summary>
		/// Sets the do-not-disturb enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public void SetDoNotDisturb(bool enabled)
		{
			CallMethod(ConferenceDeviceControlApi.METHOD_SET_DO_NOT_DISTURB, enabled);
		}

		/// <summary>
		/// Sets the auto-answer enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public void SetAutoAnswer(bool enabled)
		{
			CallMethod(ConferenceDeviceControlApi.METHOD_SET_AUTO_ANSWER, enabled);
		}

		/// <summary>
		/// Sets the privacy mute enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public void SetPrivacyMute(bool enabled)
		{
			CallMethod(ConferenceDeviceControlApi.METHOD_SET_PRIVACY_MUTE, enabled);
		}

		/// <summary>
		/// Sets the camera mute state.
		/// </summary>
		/// <param name="mute"></param>
		public void SetCameraMute(bool mute)
		{
			CallMethod(ConferenceDeviceControlApi.METHOD_SET_CAMERA_MUTE, mute);
		}

		/// <summary>
		/// Starts a personal meeting.
		/// </summary>
		public void StartPersonalMeeting()
		{
			CallMethod(ConferenceDeviceControlApi.METHOD_START_PERSONAL_MEETING);
		}

		/// <summary>
		/// Locks the current active conference so no more participants may join.
		/// </summary>
		/// <param name="enabled"></param>
		public void EnableCallLock(bool enabled)
		{
			CallMethod(ConferenceDeviceControlApi.METHOD_ENABLE_CALL_LOCK, enabled);
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
