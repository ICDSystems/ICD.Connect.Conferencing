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
		public event EventHandler<ConferenceEventArgs> OnConferenceAdded;
		public event EventHandler<ConferenceEventArgs> OnConferenceRemoved;
		public event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallAdded;
		public event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallRemoved;
		public event EventHandler<BoolEventArgs> OnDoNotDisturbChanged;
		public event EventHandler<BoolEventArgs> OnAutoAnswerChanged;
		public event EventHandler<BoolEventArgs> OnPrivacyMuteChanged;
		public event EventHandler<ConferenceControlSupportedConferenceFeaturesChangedApiEventArgs> OnSupportedConferenceFeaturesChanged;

		private bool m_AutoAnswer;
		private bool m_PrivacyMuted;
		private bool m_DoNotDisturb;
		private eConferenceFeatures m_SupportedConferenceFeatures;

		#region Properties

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
		/// Returns the features that are supported by this conference control.
		/// </summary>
		public eConferenceFeatures SupportedConferenceFeatures
		{
			get { return m_SupportedConferenceFeatures; }
			[UsedImplicitly]
			private set
			{
				if (value == m_SupportedConferenceFeatures)
					return;

				m_SupportedConferenceFeatures = value;

				OnSupportedConferenceFeaturesChanged.Raise(this,
				                                           new ConferenceControlSupportedConferenceFeaturesChangedApiEventArgs(
					                                           m_SupportedConferenceFeatures));
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
		protected AbstractProxyConferenceDeviceControl(IProxyDeviceBase parent, int id)
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
