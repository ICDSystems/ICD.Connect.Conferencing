using System;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.IncomingCalls;

namespace ICD.Connect.Conferencing.EventArguments
{
	public sealed class ConferenceControlIncomingCallEventArgs : EventArgs
	{
		private readonly IConferenceDeviceControl m_Control;
		private readonly IIncomingCall m_IncomingCall;

		/// <summary>
		/// Gets the control that instantiated the incoming call.
		/// </summary>
		public IConferenceDeviceControl Control { get { return m_Control; } }

		/// <summary>
		/// Gets the incoming call.
		/// </summary>
		public IIncomingCall IncomingCall { get { return m_IncomingCall; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="control"></param>
		/// <param name="incomingCall"></param>
		public ConferenceControlIncomingCallEventArgs(IConferenceDeviceControl control, IIncomingCall incomingCall)
		{
			m_Control = control;
			m_IncomingCall = incomingCall;
		}
	}
}
