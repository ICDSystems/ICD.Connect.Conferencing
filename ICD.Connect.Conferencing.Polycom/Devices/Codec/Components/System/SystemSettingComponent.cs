using System;
using System.Text.RegularExpressions;
using ICD.Common.Logging.LoggingContexts;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.System
{
	public sealed class SystemSettingComponent : AbstractPolycomComponent
	{
		private const string REGEX_SIP_ACCOUNT_NAME = @"systemsetting sipaccountname (?'accountName'\S+)";

		/// <summary>
		/// Raised when the sip account name changes.
		/// </summary>
		public event EventHandler<StringEventArgs> OnSipAccountNameChanged; 

		private string m_SipAccountName;

		/// <summary>
		/// Gets the sip account name reported by the system.
		/// </summary>
		[CanBeNull]
		public string SipAccountName
		{
			get { return m_SipAccountName; }
			private set
			{
				if (value == m_SipAccountName)
					return;

				m_SipAccountName = value;

				Codec.Logger.LogSetTo(eSeverity.Informational, "SIP Account Name", m_SipAccountName);

				OnSipAccountNameChanged.Raise(this, new StringEventArgs(m_SipAccountName));
			}
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="codec"></param>
		public SystemSettingComponent(PolycomGroupSeriesDevice codec)
			: base(codec)
		{
			Subscribe(Codec);

			Codec.RegisterFeedback("systemsetting", HandleSystemSetting);

			if (Codec.Initialized)
				Initialize();
		}

		/// <summary>
		/// Called to initialize the component.
		/// </summary>
		protected override void Initialize()
		{
			base.Initialize();

			Codec.EnqueueCommand("systemsetting get sipaccountname");
		}

		/// <summary>
		/// Handle systemsetting events.
		/// </summary>
		/// <param name="data"></param>
		private void HandleSystemSetting(string data)
		{
			Match match;
			if (RegexUtils.Matches(data, REGEX_SIP_ACCOUNT_NAME, out match))
				SipAccountName = match.Groups["accountName"].Value;
		}

		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("SIP Account Name", SipAccountName);
		}
	}
}
