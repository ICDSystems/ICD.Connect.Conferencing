using System;
using System.Text.RegularExpressions;
using ICD.Common.Logging.LoggingContexts;
using ICD.Common.Utils;
using ICD.Common.Utils.Comparers;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.System
{
	public sealed class SystemComponent : AbstractPolycomComponent
	{
		private const string VERSION_REGEX = @"version (?'version'\d+.\d+.\d+(.\d+)?)";

		private static readonly Version s_ExpectedVersion = new Version(6, 1, 6, 1);

		private Version m_FirmwareVersion;

		/// <summary>
		/// Gets the firmware version reported by the system.
		/// </summary>
		public Version FirmwareVersion
		{
			get { return m_FirmwareVersion; }
			private set
			{
				if (value == m_FirmwareVersion)
					return;

				m_FirmwareVersion = value;

				Codec.Logger.LogSetTo(eSeverity.Informational, "FirmwareVersion", m_FirmwareVersion);

				if (!UndefinedVersionEqualityComparer.Instance.Equals(m_FirmwareVersion, s_ExpectedVersion))
					Codec.Logger.Log(eSeverity.Warning, "Driver is programmed to work with firmware version {0}", s_ExpectedVersion);
			}
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="codec"></param>
		public SystemComponent(PolycomGroupSeriesDevice codec)
			: base(codec)
		{
			Subscribe(Codec);

			Codec.RegisterFeedback("version", HandleVersion);

			if (Codec.Initialized)
				Initialize();
		}

		/// <summary>
		/// Called to initialize the component.
		/// </summary>
		protected override void Initialize()
		{
			base.Initialize();

			Codec.EnqueueCommand("version");
		}

		private void HandleVersion(string version)
		{
			Match match;
			if (RegexUtils.Matches(version, VERSION_REGEX, out match))
				FirmwareVersion = new Version(match.Groups["version"].Value);
		}

		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Firmware Version", FirmwareVersion);
		}
	}
}