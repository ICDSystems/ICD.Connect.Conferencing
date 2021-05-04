﻿using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.Devices;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.IncomingCalls;
using ICD.Connect.Conferencing.Participants;
using ICD.Connect.Devices.Mock;

namespace ICD.Connect.Conferencing.Mock
{
	public interface IMockConferencingDevice : IVideoConferenceDevice, IMockDevice
	{
		event EventHandler<GenericEventArgs<IParticipant>> OnParticipantAdded;
		event EventHandler<GenericEventArgs<IParticipant>> OnParticipantRemoved;
		event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallAdded;
		event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallRemoved; 

		IEnumerable<IParticipant> GetSources();

		eDialContextSupport CanDial(IDialContext dialContext);

		void Dial(IDialContext dialContext);

		void StartPersonalMeeting();
	}
}
