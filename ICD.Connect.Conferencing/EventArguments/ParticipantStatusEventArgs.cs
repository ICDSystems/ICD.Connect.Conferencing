using System;
using ICD.Common.Utils.EventArguments;

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

	public static class ParticipantStatusExtensions
	{
		/// <summary>
		/// Returns true if the value is some type of connected.
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static bool GetIsOnline(this eParticipantStatus extends)
		{
			switch (extends)
			{
				case eParticipantStatus.Undefined:
				case eParticipantStatus.Idle:
				case eParticipantStatus.Dialing:
				case eParticipantStatus.Ringing:
				case eParticipantStatus.Connecting:
				case eParticipantStatus.Disconnecting:
				case eParticipantStatus.Disconnected:
					return false;

				case eParticipantStatus.Connected:
				case eParticipantStatus.OnHold:
				case eParticipantStatus.EarlyMedia:
				case eParticipantStatus.Preserved:
				case eParticipantStatus.RemotePreserved:
					return true;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}
