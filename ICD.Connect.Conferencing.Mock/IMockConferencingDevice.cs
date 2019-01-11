using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.Participants;
using ICD.Connect.Devices;

namespace ICD.Connect.Conferencing.Mock
{
	public interface IMockConferencingDevice : IDevice
	{
		event EventHandler<GenericEventArgs<ITraditionalParticipant>> OnParticipantAdded;
		event EventHandler<GenericEventArgs<ITraditionalParticipant>> OnParticipantRemoved;
		event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallAdded;
		event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallRemoved; 

		IEnumerable<ITraditionalParticipant> GetSources();

		eDialContextSupport CanDial(IDialContext dialContext);

		void Dial(IDialContext dialContext);
	}
}
