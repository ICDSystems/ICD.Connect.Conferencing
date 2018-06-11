using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Conferencing.Polycom.Devices.Components.Layout
{
	public sealed class LayoutComponent : AbstractPolycomComponent
	{
		// You cannot specify a monitor in release 4.2. This parameter is required, but ignored.
		private const string MONITOR = "monitor1";

		private static readonly BiDictionary<ePipPosition, string> s_SerialMap =
			new BiDictionary<ePipPosition, string>
		{
			{ePipPosition.LowerLeft, "pip_lower_left"},
			{ePipPosition.LowerRight, "pip_lower_right"},
			{ePipPosition.UpperLeft, "pip_upper_left"},
			{ePipPosition.UpperRight, "pip_upper_right"},
			{ePipPosition.Top, "pip_top"},
			{ePipPosition.Right, "pip_right"},
			{ePipPosition.Bottom, "pip_bottom"},
			{ePipPosition.SideBySide, "side_by_side"},
			{ePipPosition.FullScreen, "full_screen"}
		};

		/// <summary>
		/// Raised when the PIP position changes.
		/// </summary>
		public event EventHandler<PipPositionEventArgs> OnPipPositionChanged; 

		private ePipPosition m_PipPosition;

		/// <summary>
		/// Gets the PIP position.
		/// </summary>
		public ePipPosition PipPosition
		{
			get
			{
				return m_PipPosition;
			}
			private set
			{
				if (value == m_PipPosition)
					return;

				m_PipPosition = value;

				Codec.Log(eSeverity.Informational, "PipPosition set to {0}", m_PipPosition);

				OnPipPositionChanged.Raise(this, new PipPositionEventArgs(m_PipPosition));
			}
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="codec"></param>
		public LayoutComponent(PolycomGroupSeriesDevice codec)
			: base(codec)
		{
			Subscribe(Codec);

			if (Codec.Initialized)
				Initialize();
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			OnPipPositionChanged = null;

			base.Dispose(disposing);
		}

		/// <summary>
		/// Called to initialize the component.
		/// </summary>
		protected override void Initialize()
		{
			base.Initialize();

			Codec.SendCommand("configlayout {0} get", MONITOR);
		}

		/// <summary>
		/// Sets the Self View location.
		/// </summary>
		/// <param name="position"></param>
		public void SetPipPosition(ePipPosition position)
		{
			string pip = s_SerialMap.GetValue(position);

			Codec.SendCommand("configlayout {0} {1}", MONITOR, pip);
			Codec.Log(eSeverity.Informational, "Setting PIP position {0}", pip);
		}

		#region Console

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			string pipHelp = string.Format("<{0}>", StringUtils.ArrayFormat(EnumUtils.GetValues<ePipPosition>()));
			yield return new GenericConsoleCommand<ePipPosition>("SetPipPosition", pipHelp, p => SetPipPosition(p));
		}

		/// <summary>
		/// Shim to avoid "unverifiable code" warning.
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

			addRow("PipPosition", PipPosition);
		}

		#endregion
	}
}
