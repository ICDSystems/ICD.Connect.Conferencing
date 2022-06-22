using System;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Dialing
{
    public enum eCiscoCallStatus
    {
        Undefined = 0,
        Idle = 1,
        Dialing = 2,
        Ringing = 3,
        Connecting = 4,
        Connected = 5,
        Disconnecting = 6,
        // ReSharper disable once InconsistentNaming
        OnHold = 7,
        EarlyMedia = 8,
        Preserved = 9,
        RemotePreserved = 10,
        Disconnected = 11,
        Waiting = 12,
        Invited = 13,
        Observer = 14,
        Alerting = 15,
        Orphaned = 16 // Used when the call is no longer listed on the codec
    }

	public static class CiscoCallStatusExtensions
	{
		/// <summary>
		/// Returns true if the value is some type of connected.
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static bool GetIsOnline(this eCiscoCallStatus extends)
		{
			switch (extends)
			{
				case eCiscoCallStatus.Undefined:
				case eCiscoCallStatus.Idle:
				case eCiscoCallStatus.Dialing:
				case eCiscoCallStatus.Ringing:
				case eCiscoCallStatus.Invited:
				case eCiscoCallStatus.Alerting:
				case eCiscoCallStatus.Connecting:
				case eCiscoCallStatus.Disconnecting:
				case eCiscoCallStatus.Disconnected:
				case eCiscoCallStatus.Orphaned:
					return false;

				case eCiscoCallStatus.Observer:
				case eCiscoCallStatus.Waiting:
				case eCiscoCallStatus.Connected:
				case eCiscoCallStatus.OnHold:
				case eCiscoCallStatus.EarlyMedia:
				case eCiscoCallStatus.Preserved:
				case eCiscoCallStatus.RemotePreserved:
                    return true;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// Converts the Cisco call status to an equivalent conference status.
        /// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static eConferenceStatus ToConferenceStatus(this eCiscoCallStatus extends)
		{
			switch (extends)
			{
				case eCiscoCallStatus.Undefined:
					return eConferenceStatus.Undefined;

				case eCiscoCallStatus.Dialing:
				case eCiscoCallStatus.Ringing:
				case eCiscoCallStatus.Connecting:
				case eCiscoCallStatus.Invited:
				case eCiscoCallStatus.Alerting:
					return eConferenceStatus.Connecting;

				case eCiscoCallStatus.Observer:
				case eCiscoCallStatus.Waiting:
				case eCiscoCallStatus.Connected:
				case eCiscoCallStatus.EarlyMedia:
				case eCiscoCallStatus.Preserved:
				case eCiscoCallStatus.RemotePreserved:
					return eConferenceStatus.Connected;

				case eCiscoCallStatus.OnHold:
					return eConferenceStatus.OnHold;

				case eCiscoCallStatus.Disconnecting:
					return eConferenceStatus.Disconnecting;

				case eCiscoCallStatus.Disconnected:
				case eCiscoCallStatus.Idle:
				case eCiscoCallStatus.Orphaned:
					return eConferenceStatus.Disconnected;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// Converts the Cisco call status to an equivalent participant status
		/// Used for non-webex conference with a single participant
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
        public static eParticipantStatus ToParticipantStatus(this eCiscoCallStatus extends)
        {
            switch (extends)
            {
                case eCiscoCallStatus.Undefined:
                    return eParticipantStatus.Undefined;
                case eCiscoCallStatus.Idle:
                    return eParticipantStatus.Idle;
                case eCiscoCallStatus.Dialing:
                    return eParticipantStatus.Dialing;
                case eCiscoCallStatus.Ringing:
                    return eParticipantStatus.Ringing;
                case eCiscoCallStatus.Invited:
                    return eParticipantStatus.Invited;
                case eCiscoCallStatus.Alerting:
                    return eParticipantStatus.Alerting;
                case eCiscoCallStatus.Connecting:
                    return eParticipantStatus.Connecting;
                case eCiscoCallStatus.Disconnecting:
                    return eParticipantStatus.Disconnecting;
                case eCiscoCallStatus.Observer:
                    return eParticipantStatus.Observer;
                case eCiscoCallStatus.Waiting:
                    return eParticipantStatus.Waiting;
                case eCiscoCallStatus.Connected:
                    return eParticipantStatus.Connected;
                case eCiscoCallStatus.OnHold:
                    return eParticipantStatus.OnHold;
                case eCiscoCallStatus.EarlyMedia:
                    return eParticipantStatus.EarlyMedia;
                case eCiscoCallStatus.Preserved:
                    return eParticipantStatus.Preserved;
                case eCiscoCallStatus.RemotePreserved:
                    return eParticipantStatus.RemotePreserved;
                case eCiscoCallStatus.Disconnected:
                case eCiscoCallStatus.Orphaned: // Only mapping that isn't 1-1
                    return eParticipantStatus.Disconnected;

				default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
