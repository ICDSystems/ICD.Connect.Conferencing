using System;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.Controls;

namespace ICD.Connect.Conferencing.Server
{
	public class DialerSourceEventArgs : EventArgs
	{
		public IDialingDeviceControl Dialer { get; private set; }
		public IConferenceSource Source { get; private set; }

		public DialerSourceEventArgs(IDialingDeviceControl dialer, IConferenceSource source)
		{
			Dialer = dialer;
			Source = source;
		}
	}
}