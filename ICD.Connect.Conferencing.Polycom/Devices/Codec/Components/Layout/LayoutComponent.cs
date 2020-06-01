using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Common.Logging.LoggingContexts;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Layout
{
	public sealed class LayoutComponent : AbstractPolycomComponent
	{
		// You cannot specify a monitor in release 4.2. This parameter is required, but ignored.
		private const string MONITOR = "monitor1";

		private const string SELFVIEW_REGEX = @"systemsetting selfview (?'selfview'\S+)";

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

		private static readonly BiDictionary<eSelfView, string> s_SelfViewMap =
			new BiDictionary<eSelfView, string>
			{
				{eSelfView.On, "on"},
				{eSelfView.Off, "off"},
				{eSelfView.Auto, "auto"},
			};

		/// <summary>
		/// Raised when the PIP position changes.
		/// </summary>
		public event EventHandler<PipPositionEventArgs> OnPipPositionChanged;

		/// <summary>
		/// Raised when the self view mode changes.
		/// </summary>
		public event EventHandler<SelfViewEventArgs> OnSelfViewChanged; 

		private ePipPosition m_PipPosition;
		private eSelfView m_SelfView;

		#region Properties

		/// <summary>
		/// Gets the PIP position.
		/// </summary>
		public ePipPosition PipPosition
		{
			get { return m_PipPosition; }
			private set
			{
				if (value == m_PipPosition)
					return;

				m_PipPosition = value;

				Codec.Logger.LogSetTo(eSeverity.Informational, "PipPosition", m_PipPosition);

				OnPipPositionChanged.Raise(this, new PipPositionEventArgs(m_PipPosition));
			}
		}

		/// <summary>
		/// Gets the self view mode.
		/// </summary>
		public eSelfView SelfView
		{
			get { return m_SelfView; }
			private set
			{
				if (value == m_SelfView)
					return;

				m_SelfView = value;

				Codec.Logger.LogSetTo(eSeverity.Informational, "SelfView", m_SelfView);

				OnSelfViewChanged.Raise(this, new SelfViewEventArgs(m_SelfView));
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="codec"></param>
		public LayoutComponent(PolycomGroupSeriesDevice codec)
			: base(codec)
		{
			Subscribe(Codec);

			codec.RegisterFeedback("systemsetting", HandleSystemSetting);

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

			Codec.EnqueueCommand("configlayout {0} get", MONITOR);
			Codec.EnqueueCommand("systemsetting get selfview");
		}

		#region Methods

		/// <summary>
		/// Sets the Self View location.
		/// </summary>
		/// <param name="position"></param>
		public void SetPipPosition(ePipPosition position)
		{
			string pip = s_SerialMap.GetValue(position);

			Codec.EnqueueCommand("configlayout {0} {1}", MONITOR, pip);
			Codec.Logger.Log(eSeverity.Informational, "Setting PIP position {0}", pip);
		}

		/// <summary>
		/// Sets the self view mode.
		/// </summary>
		/// <param name="selfView"></param>
		public void SetSelfView(eSelfView selfView)
		{
			string name = s_SelfViewMap.GetValue(selfView);

			Codec.EnqueueCommand("systemsetting selfview {0}", name);
			Codec.Logger.Log(eSeverity.Informational, "Setting SelfView {0}", name);
		}

		#endregion

		/// <summary>
		/// Handle systemsetting events.
		/// </summary>
		/// <param name="data"></param>
		private void HandleSystemSetting(string data)
		{
			Match match = Regex.Match(data, SELFVIEW_REGEX);
			if (!match.Success)
				return;

			string name = match.Groups["selfview"].Value;

			eSelfView selfView;
			if (s_SelfViewMap.TryGetKey(name, out selfView))
				SelfView = selfView;
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

			string pipHelp = string.Format("SetPipPosition <{0}>", StringUtils.ArrayFormat(EnumUtils.GetValues<ePipPosition>()));
			yield return new GenericConsoleCommand<ePipPosition>("SetPipPosition", pipHelp, p => SetPipPosition(p));

			string selfViewHelp = string.Format("SetSelfView <{0}>", StringUtils.ArrayFormat(EnumUtils.GetValues<eSelfView>()));
			yield return new GenericConsoleCommand<eSelfView>("SetSelfView", selfViewHelp, s => SetSelfView(s));
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
