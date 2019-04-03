using System;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Telemetry.Attributes;

namespace ICD.Connect.Conferencing.Controls.Dialing
{
	[ExternalTelemetry("Sip Telemetry", typeof(SipDialingDeviceExternalTelemetryProvider))]
	public interface ISipDialingDeviceControl : IDialingDeviceControl
	{
		event EventHandler<BoolEventArgs> OnSipEnabledChanged;
		event EventHandler<StringEventArgs> OnSipLocalNameChanged;
		event EventHandler<StringEventArgs> OnSipRegistrationStatusChanged; 

		bool SipIsRegistered { get; }
		string SipLocalName { get; }
		string SipRegistrationStatus { get; }
	}
}