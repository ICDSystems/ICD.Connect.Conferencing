using System;

namespace ICD.Connect.Conferencing.Devices
{
	public interface IInterpretationDevice : IDialerDevice
	{
		event EventHandler OnInterpretationActiveChanged;
		
		bool IsInterpretationActive { get; }
	}
}
