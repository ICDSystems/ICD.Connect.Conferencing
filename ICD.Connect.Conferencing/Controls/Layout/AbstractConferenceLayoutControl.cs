using System;
using System.Collections.Generic;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Devices;
using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Conferencing.Controls.Layout
{
	public abstract class AbstractConferenceLayoutControl<TParent> : AbstractDeviceControl<TParent>, IConferenceLayoutControl
		where TParent : IDeviceBase
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
			protected set
			{
				if (value == m_LayoutAvailable)
					return;

				m_LayoutAvailable = value;

				Log(eSeverity.Informational, "LayoutAvailable set to {0}", m_LayoutAvailable);

				OnLayoutAvailableChanged.Raise(this, new ConferenceLayoutAvailableApiEventArgs(m_LayoutAvailable));
			}
		}

		/// <summary>
		/// Gets the self view enabled state.
		/// </summary>
		public bool SelfViewEnabled
		{
			get { return m_SelfViewEnabled; }
			protected set
			{
				if (value == m_SelfViewEnabled)
					return;

				m_SelfViewEnabled = value;

				Log(eSeverity.Informational, "SelfViewEnabled set to {0}", m_SelfViewEnabled);

				OnSelfViewEnabledChanged.Raise(this, new ConferenceLayoutSelfViewApiEventArgs(m_SelfViewEnabled));
			}
		}

		/// <summary>
		/// Gets the self view fullscreen enabled state.
		/// </summary>
		public bool SelfViewFullScreenEnabled
		{
			get { return m_SelfViewFullScreenEnabled; }
			protected set
			{
				if (value == m_SelfViewFullScreenEnabled)
					return;

				m_SelfViewFullScreenEnabled = value;

				Log(eSeverity.Informational, "SelfViewFullScreenEnabled set to {0}", m_SelfViewFullScreenEnabled);
				
				OnSelfViewFullScreenEnabledChanged.Raise(this, new ConferenceLayoutSelfViewFullScreenApiEventArgs(m_SelfViewFullScreenEnabled));
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		protected AbstractConferenceLayoutControl(TParent parent, int id)
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
		public abstract void SetSelfViewEnabled(bool enabled);

		/// <summary>
		/// Enables/disables the self-view fullscreen mode during video conference.
		/// </summary>
		/// <param name="enabled"></param>
		public abstract void SetSelfViewFullScreenEnabled(bool enabled);

		/// <summary>
		/// Sets the arrangement of UI windows for the video conference.
		/// </summary>
		/// <param name="mode"></param>
		public abstract void SetLayoutMode(eLayoutMode mode);

		#endregion

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
