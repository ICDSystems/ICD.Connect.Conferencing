using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Info;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Controls.Layout;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Devices.Proxies.Controls;
using ICD.Connect.Devices.Proxies.Devices;

namespace ICD.Connect.Conferencing.Proxies.Controls.Layout
{
	public abstract class AbstractProxyConferenceLayoutControl : AbstractProxyDeviceControl, IProxyConferenceLayoutControl
	{
		/// <summary>
		/// Raised when layout control becomes available/unavailable.
		/// </summary>
		public event EventHandler<ConferenceLayoutAvailableApiEventArgs> OnLayoutAvailableChanged;

		/// <summary>
		/// Raised when the self view enabled state changes.
		/// </summary>
		public event EventHandler<ConferenceLayoutSelfViewApiEventArgs> OnSelfViewEnabledChanged;

		/// <summary>
		/// Raised when the self view full screen enabled state changes.
		/// </summary>
		public event EventHandler<ConferenceLayoutSelfViewFullScreenApiEventArgs> OnSelfViewFullScreenEnabledChanged;

		private bool m_SelfViewEnabled;
		private bool m_SelfViewFullScreenEnabled;
		private bool m_LayoutAvailable;

		#region Properties

		/// <summary>
		/// Returns true if layout control is currently available.
		/// Some conferencing devices only support layout in certain configurations (e.g. single display mode).
		/// </summary>
		public bool LayoutAvailable
		{
			get { return m_LayoutAvailable; }
			[UsedImplicitly]
			private set
			{
				if (value == m_LayoutAvailable)
					return;

				m_LayoutAvailable = value;

				Logger.Set("Layout Available", eSeverity.Informational, m_LayoutAvailable);

				OnLayoutAvailableChanged.Raise(this, new ConferenceLayoutAvailableApiEventArgs(m_LayoutAvailable));
			}
		}

		/// <summary>
		/// Gets the self view enabled state.
		/// </summary>
		public bool SelfViewEnabled
		{
			get { return m_SelfViewEnabled; }
			[UsedImplicitly]
			private set
			{
				if (value == m_SelfViewEnabled)
					return;

				m_SelfViewEnabled = value;

				Logger.Set("Self View Enabled", eSeverity.Informational, m_SelfViewEnabled);

				OnSelfViewEnabledChanged.Raise(this, new ConferenceLayoutSelfViewApiEventArgs(m_SelfViewEnabled));
			}
		}

		/// <summary>
		/// Gets the self view fullscreen enabled state.
		/// </summary>
		public bool SelfViewFullScreenEnabled
		{
			get { return m_SelfViewFullScreenEnabled; }
			[UsedImplicitly]
			private set
			{
				if (value == m_SelfViewFullScreenEnabled)
					return;

				m_SelfViewFullScreenEnabled = value;

				Logger.Set("Self View Fullscreen Enabled", eSeverity.Informational, m_SelfViewFullScreenEnabled);

				OnSelfViewFullScreenEnabledChanged.Raise(this, new ConferenceLayoutSelfViewFullScreenApiEventArgs(m_SelfViewFullScreenEnabled));
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		protected AbstractProxyConferenceLayoutControl(IProxyDeviceBase parent, int id)
			: base(parent, id)
		{
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnLayoutAvailableChanged = null;
			OnSelfViewEnabledChanged = null;
			OnSelfViewFullScreenEnabledChanged = null;

			base.DisposeFinal(disposing);
		}

		#region Methods

		/// <summary>
		/// Enables/disables the self-view window during video conference.
		/// </summary>
		/// <param name="enabled"></param>
		public void SetSelfViewEnabled(bool enabled)
		{
			CallMethod(ConferenceLayoutControlApi.METHOD_SET_SELF_VIEW_ENABLED, enabled);
		}

		/// <summary>
		/// Enables/disables the self-view fullscreen mode during video conference.
		/// </summary>
		/// <param name="enabled"></param>
		public void SetSelfViewFullScreenEnabled(bool enabled)
		{
			CallMethod(ConferenceLayoutControlApi.METHOD_SET_SELF_VIEW_FULL_SCREEN_ENABLED, enabled);
		}

		/// <summary>
		/// Sets the arrangement of UI windows for the video conference.
		/// </summary>
		/// <param name="mode"></param>
		public void SetLayoutMode(eLayoutMode mode)
		{
			CallMethod(ConferenceLayoutControlApi.METHOD_SET_LAYOUT_MODE, mode);
		}

		#endregion

		/// <summary>
		/// Override to build initialization commands on top of the current class info.
		/// </summary>
		/// <param name="command"></param>
		protected override void Initialize(ApiClassInfo command)
		{
			base.Initialize(command);

			ApiCommandBuilder.UpdateCommand(command)
			                 .SubscribeEvent(ConferenceLayoutControlApi.EVENT_LAYOUT_AVAILABLE)
			                 .SubscribeEvent(ConferenceLayoutControlApi.EVENT_SELF_VIEW_ENABLED)
			                 .SubscribeEvent(ConferenceLayoutControlApi.EVENT_SELF_VIEW_FULL_SCREEN_ENABLED)
			                 .GetProperty(ConferenceLayoutControlApi.PROPERTY_LAYOUT_AVAILABLE)
			                 .GetProperty(ConferenceLayoutControlApi.PROPERTY_SELF_VIEW_ENABLED)
			                 .GetProperty(ConferenceLayoutControlApi.PROPERTY_SELF_VIEW_FULL_SCREEN_ENABLED)
			                 .Complete();
		}

		#region Console

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			foreach (IConsoleNodeBase node in ConferenceLayoutControlConsole.GetConsoleNodes(this))
				yield return node;
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			ConferenceLayoutControlConsole.BuildConsoleStatus(this, addRow);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			foreach (IConsoleCommand command in ConferenceLayoutControlConsole.GetConsoleCommands(this))
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
