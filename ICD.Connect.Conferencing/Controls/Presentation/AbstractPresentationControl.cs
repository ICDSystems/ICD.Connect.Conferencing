using System;
using System.Collections.Generic;
using ICD.Common.Logging.Activities;
using ICD.Common.Logging.LoggingContexts;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Devices;
using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Conferencing.Controls.Presentation
{
	public abstract class AbstractPresentationControl<TDevice> : AbstractDeviceControl<TDevice>, IPresentationControl
		where TDevice : IDevice
	{
		/// <summary>
		/// Raised when the presentation active input changes.
		/// </summary>
		public event EventHandler<PresentationActiveInputApiEventArgs> OnPresentationActiveInputChanged;

		/// <summary>
		/// Raised when the presentation active state changes.
		/// </summary>
		public event EventHandler<PresentationActiveApiEventArgs> OnPresentationActiveChanged;
		
		private int? m_PresentationActiveInput;
		private bool m_PresentationActive;

		/// <summary>
		/// Gets the active presentation input.
		/// </summary>
		public int? PresentationActiveInput
		{
			get { return m_PresentationActiveInput; }
			protected set
			{
				if (value == m_PresentationActiveInput)
					return;

				m_PresentationActiveInput = value;

				Logger.LogSetTo(eSeverity.Informational, "PresentationActiveInput", m_PresentationActiveInput);

				OnPresentationActiveInputChanged.Raise(this, new PresentationActiveInputApiEventArgs(m_PresentationActiveInput));
			}
		}

		/// <summary>
		/// Gets the active presentation state.
		/// </summary>
		public bool PresentationActive
		{
			get { return m_PresentationActive; }
			protected set
			{
				try
				{
					if (value == m_PresentationActive)
						return;

					m_PresentationActive = value;

					Logger.LogSetTo(eSeverity.Informational, "PresentationActive", m_PresentationActive);

					OnPresentationActiveChanged.Raise(this, new PresentationActiveApiEventArgs(m_PresentationActive));
				}
				finally
				{
					Activities.LogActivity(m_PresentationActive
					   ? new Activity(Activity.ePriority.Medium, "Presentation Active", "Presentation Active",
									  eSeverity.Informational)
					   : new Activity(Activity.ePriority.Lowest, "Presentation Active", "Presentation Inactive",
									  eSeverity.Informational));
				}
			}
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		protected AbstractPresentationControl(TDevice parent, int id)
			: base(parent, id)
		{
			// Initialize activities
			PresentationActive = false;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		/// <param name="uuid"></param>
		protected AbstractPresentationControl(TDevice parent, int id, Guid uuid)
			: base(parent, id, uuid)
		{
			// Initialize activities
			PresentationActive = false;
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnPresentationActiveInputChanged = null;

			base.DisposeFinal(disposing);
		}

		#region Methods

		/// <summary>
		/// Starts presenting the source at the given input address.
		/// </summary>
		/// <param name="input"></param>
		public abstract void StartPresentation(int input);

		/// <summary>
		/// Stops the active presentation.
		/// </summary>
		public abstract void StopPresentation();

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

			foreach (IConsoleNodeBase node in PresentationControlConsole.GetConsoleNodes(this))
				yield return node;
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			PresentationControlConsole.BuildConsoleStatus(this, addRow);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			foreach (IConsoleCommand command in PresentationControlConsole.GetConsoleCommands(this))
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
