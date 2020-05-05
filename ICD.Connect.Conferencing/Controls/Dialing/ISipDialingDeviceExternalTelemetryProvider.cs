using System;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Telemetry;
using ICD.Connect.Telemetry.Attributes;
using ICD.Connect.Telemetry.Nodes.External;

namespace ICD.Connect.Conferencing.Controls.Dialing
{
	public interface ISipDialingDeviceExternalTelemetryProvider : IExternalTelemetryProvider
	{

		/// <summary>
		/// Raised when the local name of the sip dialer changes.
		/// </summary>
		[EventTelemetry(DialingTelemetryNames.SIP_LOCAL_NAME_CHANGED)]
		event EventHandler<StringEventArgs> OnSipLocalNameChanged;

		/// <summary>
		/// Raised when the sip registration status to or from "OK"
		/// </summary>
		[EventTelemetry(DialingTelemetryNames.SIP_ENABLED_CHANGED)]
		event EventHandler<BoolEventArgs> OnSipEnabledChanged;

		/// <summary>
		/// Raised when the sip registration status changes values
		/// </summary>
		[EventTelemetry(DialingTelemetryNames.SIP_STATUS_CHANGED)]
		event EventHandler<StringEventArgs> OnSipStatusChanged; 

		/// <summary>
		/// Gets a boolean representing if sip is reporting a good registration.
		/// </summary>
		[PropertyTelemetry(DialingTelemetryNames.SIP_ENABLED, null, DialingTelemetryNames.SIP_ENABLED_CHANGED)]
		bool SipEnabled { get; }

		/// <summary>
		/// Gets the status of the sip registration
		/// </summary>
		[PropertyTelemetry(DialingTelemetryNames.SIP_STATUS, null, DialingTelemetryNames.SIP_STATUS_CHANGED)]
		string SipStatus { get; }

		/// <summary>
		/// Gets the sip URI for this dialer.
		/// </summary>
		[PropertyTelemetry(DialingTelemetryNames.SIP_LOCAL_NAME, null, DialingTelemetryNames.SIP_LOCAL_NAME_CHANGED)]
		string SipName { get; }
	}
}