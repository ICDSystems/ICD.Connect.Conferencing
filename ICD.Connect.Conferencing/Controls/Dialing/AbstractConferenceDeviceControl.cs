﻿using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.IncomingCalls;
using ICD.Connect.Devices;
using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Conferencing.Controls.Dialing
{
	public abstract class AbstractConferenceDeviceControl<T, TConference> : AbstractDeviceControl<T>, IConferenceDeviceControl<TConference>
		where T : IDeviceBase where TConference : IConference
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
		public abstract event EventHandler<ConferenceEventArgs> OnConferenceAdded;

		/// <summary>
		/// Raised when a conference is removed from the dialing control.
		/// </summary>
		public abstract event EventHandler<ConferenceEventArgs> OnConferenceRemoved;

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
		/// Raised when the supported conference features change.
		/// </summary>
		public event EventHandler<ConferenceControlSupportedConferenceFeaturesChangedApiEventArgs> OnSupportedConferenceFeaturesChanged;

		private readonly SafeCriticalSection m_StateSection;

		private bool m_AutoAnswer;
		private bool m_PrivacyMuted;
		private bool m_DoNotDisturb;
		private eConferenceFeatures m_SupportedConferenceFeatures;

		#region Properties

		/// <summary>
		/// Gets the type of conference this dialer supports.
		/// </summary>
		public abstract eCallType Supports { get; }

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

					Log(eSeverity.Informational, "AutoAnswer set to {0}", m_AutoAnswer);
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

					Log(eSeverity.Informational, "PrivacyMuted set to {0}", m_PrivacyMuted);
				}
				finally
				{
					m_StateSection.Leave();
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

					Log(eSeverity.Informational, "DoNotDisturb set to {0}", m_DoNotDisturb);
				}
				finally
				{
					m_StateSection.Leave();
				}

				OnDoNotDisturbChanged.Raise(this, new BoolEventArgs(m_DoNotDisturb));
			}
		}

		/// <summary>
		/// Returns the features that are supported by this conference control.
		/// </summary>
		public eConferenceFeatures SupportedConferenceFeatures
		{
			get { return m_SupportedConferenceFeatures; }
			protected set
			{
				if (value == m_SupportedConferenceFeatures)
					return;

				m_SupportedConferenceFeatures = value;

				OnSupportedConferenceFeaturesChanged.Raise(this,
				                                           new ConferenceControlSupportedConferenceFeaturesChangedApiEventArgs(
					                                           m_SupportedConferenceFeatures));
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
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnDoNotDisturbChanged = null;
			OnAutoAnswerChanged = null;
			OnPrivacyMuteChanged = null;
			OnSupportedConferenceFeaturesChanged = null;

			base.DisposeFinal(disposing);
		}

		#region Methods

		/// <summary>
		/// Gets the active conference sources.
		/// </summary>
		/// <returns></returns>
		IEnumerable<IConference> IConferenceDeviceControl.GetConferences()
		{
			return GetConferences().Cast<IConference>();
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
