using System;
using ICD.Connect.Conferencing.Devices;

namespace ICD.Connect.Conferencing.Server.Devices.Client
{
	public interface IClientInterpretationDevice : IDialerDevice
	{
		event EventHandler OnInterpretationActiveChanged;
		
		bool IsInterpretationActive { get; }
	}
}
