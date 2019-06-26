using System.Collections.Generic;
using System.Text.RegularExpressions;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Presentation
{
	public enum eMonitor
	{
		Monitor1,
		Monitor2,
		Monitor3
	}

	public enum eConfigPresentationMode
	{
		Auto,
		Near,
		Far,
		NearOrFar,
		Content,
		ContentOrNear,
		ContentOrFar,
		RecAll,
		RecFarOrNear,
		All
	}

	public sealed class PresentationComponent : AbstractPolycomComponent
	{
		private const string CONFIG_PRESENTATION_REGEX = @"configpresentation (?'monitor'monitor\d+):(?'mode'.*)";

		private static readonly BiDictionary<eMonitor, string> s_MonitorStrings =
			new BiDictionary<eMonitor, string>
			{
				{eMonitor.Monitor1, "monitor1"},
				{eMonitor.Monitor2, "monitor2"},
				{eMonitor.Monitor3, "monitor3"}
			};

		private static readonly BiDictionary<eConfigPresentationMode, string> s_ConfigPresentationModeStrings =
			new BiDictionary<eConfigPresentationMode, string>
			{
				{eConfigPresentationMode.Auto, "auto"},
				{eConfigPresentationMode.Near, "near"},
				{eConfigPresentationMode.Far, "far"},
				{eConfigPresentationMode.NearOrFar, "near-or-far"},
				{eConfigPresentationMode.Content, "content"},
				{eConfigPresentationMode.ContentOrNear, "content-or-near"},
				{eConfigPresentationMode.ContentOrFar, "content-or-far"},
				{eConfigPresentationMode.RecAll, "rec-all"},
				{eConfigPresentationMode.RecFarOrNear, "rec-far-or-near"},
				{eConfigPresentationMode.All, "all"}
			};

		private readonly Dictionary<eMonitor, eConfigPresentationMode> m_MonitorPresentationMode;
		private readonly SafeCriticalSection m_CriticalSection;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="codec"></param>
		public PresentationComponent(PolycomGroupSeriesDevice codec)
			: base(codec)
		{
			m_MonitorPresentationMode = new Dictionary<eMonitor, eConfigPresentationMode>();
			m_CriticalSection = new SafeCriticalSection();

			Subscribe(Codec);

			Codec.RegisterFeedback("configpresentation", HandleConfigPresentationFeedback);

			if (Codec.Initialized)
				Initialize();
		}

		#region Methods

		/// <summary>
		/// Called to initialize the component.
		/// </summary>
		protected override void Initialize()
		{
			base.Initialize();

			Codec.EnqueueCommand("configpresentation get");
		}

		/// <summary>
		/// Sets the config presentation mode for the given monitor.
		/// </summary>
		/// <param name="monitor"></param>
		/// <param name="presentationMode"></param>
		public void SetMonitorPresentationMode(eMonitor monitor, eConfigPresentationMode presentationMode)
		{
			string monitorString = s_MonitorStrings.GetValue(monitor);
			string presentationModeString = s_ConfigPresentationModeStrings.GetValue(presentationMode);

			Codec.EnqueueCommand("configpresentation {0} {1}", monitorString, presentationModeString);
			Codec.EnqueueCommand("configpresentation {0} get", monitorString, presentationModeString);
		}

		/// <summary>
		/// Gets the current config presentation mode for the given monitor.
		/// </summary>
		/// <param name="monitor"></param>
		public eConfigPresentationMode GetMonitorPresentationMode(eMonitor monitor)
		{
			return m_CriticalSection.Execute(() => m_MonitorPresentationMode.GetDefault(monitor));
		}

		#endregion

		private void HandleConfigPresentationFeedback(string data)
		{
			// configpresentation monitor1:all

			Match match = Regex.Match(data, CONFIG_PRESENTATION_REGEX);
			if (!match.Success)
				return;

			string monitorValue = match.Groups["monitor"].Value;
			string presentationModeValue = match.Groups["mode"].Value;

			eMonitor monitor = s_MonitorStrings.GetKey(monitorValue);
			eConfigPresentationMode mode = s_ConfigPresentationModeStrings.GetKey(presentationModeValue);

			m_MonitorPresentationMode[monitor] = mode;
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

			string setMonitorPresentationModeHelp =
				string.Format("SetMonitorPresentationMode <{0}> <{1}>",
				              StringUtils.ArrayFormat(EnumUtils.GetValues<eMonitor>()),
				              StringUtils.ArrayFormat(EnumUtils.GetValues<eConfigPresentationMode>()));

			yield return new GenericConsoleCommand<eMonitor, eConfigPresentationMode>("SetMonitorPresentationMode",
			                                                                          setMonitorPresentationModeHelp,
			                                                                          (m, p) =>
				                                                                          SetMonitorPresentationMode(m, p));

			yield return new ConsoleCommand("PrintMonitors",
			                                "Prints a table of the monitors and their presentation modes",
			                                () => PrintMonitors());
		}

		private string PrintMonitors()
		{
			TableBuilder builder = new TableBuilder("Monitor", "Presentation Mode");

			foreach (eMonitor monitor in EnumUtils.GetValues<eMonitor>())
			{
				eConfigPresentationMode mode = GetMonitorPresentationMode(monitor);
				builder.AddRow(monitor, mode);
			}

			return builder.ToString();
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
