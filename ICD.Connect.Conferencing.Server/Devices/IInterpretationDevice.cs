using System;
using ICD.Connect.Conferencing.Devices;

namespace ICD.Connect.Conferencing.Server.Devices
{
	public interface IInterpretationDevice : IDialerDevice
	{
		event EventHandler OnInterpretationActiveChanged;
		
		bool IsInterpretationActive { get; }
	}
}
