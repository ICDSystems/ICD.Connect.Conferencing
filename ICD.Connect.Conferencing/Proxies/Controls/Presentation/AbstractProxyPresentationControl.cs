using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Info;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Controls.Presentation;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Devices.Proxies.Controls;
using ICD.Connect.Devices.Proxies.Devices;

namespace ICD.Connect.Conferencing.Proxies.Controls.Presentation
{
	public abstract class AbstractProxyPresentationControl : AbstractProxyDeviceControl, IProxyPresentationControl
	{
		/// <summary>
		/// Raised when the presentation active state changes.
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
			[UsedImplicitly] private set
			{
				if (value == m_PresentationActiveInput)
					return;
				
				m_PresentationActiveInput = value;

				Log(eSeverity.Informational, "PresentationActiveInput set to {0}", m_PresentationActiveInput);

				OnPresentationActiveInputChanged.Raise(this, new PresentationActiveInputApiEventArgs(m_PresentationActiveInput));
			}
		}

		/// <summary>
		/// Gets the active presentation state.
		/// </summary>
		public bool PresentationActive
		{
			get { return m_PresentationActive; }
			[UsedImplicitly]
			private set
			{
				if (value == m_PresentationActive)
					return;

				m_PresentationActive = value;

				Log(eSeverity.Informational, "PresentationActive set to {0}", m_PresentationActive);

				OnPresentationActiveChanged.Raise(this, new PresentationActiveApiEventArgs(m_PresentationActive));
			}
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		protected AbstractProxyPresentationControl(IProxyDeviceBase parent, int id)
			: base(parent, id)
		{
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnPresentationActiveInputChanged = null;
			OnPresentationActiveChanged = null;

			base.DisposeFinal(disposing);
		}

		#region Methods

		/// <summary>
		/// Starts presenting the source at the given input address.
		/// </summary>
		/// <param name="input"></param>
		public void StartPresentation(int input)
		{
			CallMethod(PresentationControlApi.METHOD_START_PRESENTATION, input);
		}

		/// <summary>
		/// Stops the active presentation.
		/// </summary>
		public void StopPresentation()
		{
			CallMethod(PresentationControlApi.METHOD_STOP_PRESENTATION);
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
							 .SubscribeEvent(PresentationControlApi.EVENT_PRESENTATION_ACTIVE_INPUT)
							 .SubscribeEvent(PresentationControlApi.EVENT_PRESENTATION_ACTIVE)
							 .GetProperty(PresentationControlApi.PROPERTY_PRESENTATION_ACTIVE_INPUT)
							 .GetProperty(PresentationControlApi.PROPERTY_PRESENTATION_ACTIVE)
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
