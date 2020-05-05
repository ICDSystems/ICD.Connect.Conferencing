using System;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Telemetry;
using ICD.Connect.Telemetry.Attributes;
using ICD.Connect.Telemetry.Nodes.External;

namespace ICD.Connect.Conferencing.Controls.Dialing
{
	public interface IDialingDeviceExternalTelemetryProvider : IExternalTelemetryProvider
	{
		#region Events

		/// <summary>
		/// Raised when the dialing device starts a call from idle state or ends the last remaining call
		/// </summary>
		[EventTelemetry(DialingTelemetryNames.CALL_IN_PROGRESS_CHANGED)]
		event EventHandler<BoolEventArgs> OnCallInProgressChanged;

		/// <summary>
		/// Raised when the dialing device adds or removes a call.
		/// </summary>
		[EventTelemetry(DialingTelemetryNames.CALL_TYPE_CHANGED)]
		event EventHandler<StringEventArgs> OnCallTypeChanged;

		/// <summary>
		/// Raised when the dialing device adds or removes a call.
		/// </summary>
		[EventTelemetry(DialingTelemetryNames.CALL_NUMBER_CHANGED)]
		event EventHandler<StringEventArgs> OnCallNumberChanged;

		#endregion

		#region Properties

		/// <summary>
		/// Gets whether the dialing device has a call in progress
		/// </summary>
		[PropertyTelemetry(DialingTelemetryNames.CALL_IN_PROGRESS, null, DialingTelemetryNames.CALL_IN_PROGRESS_CHANGED)]
		bool CallInProgress { get; }

		/// <summary>
		/// Gets a comma separated list of the type of each active call on the dialer.
		/// </summary>
		[PropertyTelemetry(DialingTelemetryNames.CALL_TYPE, null, DialingTelemetryNames.CALL_TYPE_CHANGED)]
		string CallTypes { get; }

		/// <summary>
		/// Gets a comma separated list of the type of each active call on the dialer.
		/// </summary>
		[PropertyTelemetry(DialingTelemetryNames.CALL_NUMBER, null, DialingTelemetryNames.CALL_NUMBER_CHANGED)]
		string CallNumbers { get; } 

		#endregion

		#region Methods

		/// <summary>
		/// Forces all calls on the dialer to end.
		/// </summary>
		[MethodTelemetry(DialingTelemetryNames.END_CALL_COMMAND)]
		void EndCalls();

		#endregion
	}
}