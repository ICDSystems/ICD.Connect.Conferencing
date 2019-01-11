﻿using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Conferencing.EventArguments
{
	// TODO - Currently coupled to Cisco Codec output, need to remove unused items
	public enum eParticipantStatus
	{
		Undefined = 0,
		Idle = 1,
		Dialing = 2,
		Ringing = 3,
		Connecting = 4,
		Connected = 5,
		Disconnecting = 6,
// ReSharper disable InconsistentNaming
		OnHold = 7,
// ReSharper restore InconsistentNaming
		EarlyMedia = 8,
		Preserved = 9,
		RemotePreserved = 10,
		Disconnected = 11
	}

	public sealed class ParticipantStatusEventArgs : GenericEventArgs<eParticipantStatus>
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="status"></param>
		public ParticipantStatusEventArgs(eParticipantStatus status) : base(status)
		{
		}
	}
}