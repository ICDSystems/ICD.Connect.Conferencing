using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.Controls;
using ICD.Connect.Devices;

namespace ICD.Connect.Conferencing.Server
{
	public interface IInterpretationAdapter : IDevice
	{
		event EventHandler<GenericEventArgs<IDialingDeviceControl>> OnDialerAdded;
		event EventHandler<GenericEventArgs<IDialingDeviceControl>> OnDialerRemoved;

		Dictionary<IDialingDeviceClientControl, string> GetDialingControls();
	}
}
