using System;
using ICD.Common.Properties;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Dialing;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Controls.Conference
{
	/// <summary>
	/// Call Type
	/// </summary>
	public enum eCiscoCallType
	{
		// Ignore missing comments warning
#pragma warning disable 1591
		Unknown,
		Video,
		Audio,
		AudioCanEscalate,
		ForwardAllCall
#pragma warning restore 1591
	}

	public static class CiscoCallTypeExtensions
	{
		public static eCallType ToCallType(this eCiscoCallType callType)
		{
			switch (callType)
			{
				case eCiscoCallType.Video:
					return eCallType.Video;

				case eCiscoCallType.Audio:
				case eCiscoCallType.AudioCanEscalate:
				case eCiscoCallType.ForwardAllCall:
					return eCallType.Audio;

				case eCiscoCallType.Unknown:
					return eCallType.Unknown;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}

	public sealed class CiscoConference : AbstractConference<CiscoParticipant>
	{
		private readonly DialingComponent m_DialingComponent;
		private readonly CallStatus m_CallStatus;

		private CiscoParticipant m_Participant;

		#region Constructor

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="dialingComponent"></param>
		/// <param name="callStatus"></param>
		public CiscoConference([NotNull] DialingComponent dialingComponent, CallStatus callStatus)
		{
			if (dialingComponent == null)
				throw new ArgumentNullException("dialingComponent");

			m_DialingComponent = dialingComponent;
			m_CallStatus = callStatus;
		}

		#endregion

		#region Methods

		public void InitializeConference()
		{
			m_Participant = new CiscoParticipant(m_CallStatus, m_DialingComponent);
			Subscribe(m_Participant);
			AddParticipant(m_Participant);
		}

		protected override void DisposeFinal()
		{
			Unsubscribe(m_Participant);

			base.DisposeFinal();
		}

		public override void EndConference()
		{
			base.EndConference();

			m_DialingComponent.Hangup(m_CallStatus);
		}

		public override void LeaveConference()
		{
			throw new NotSupportedException();
		}


		#endregion

		#region Participant Callbacks

		private void Subscribe(CiscoParticipant participant)
		{
			participant.OnStatusChanged += ParticipantOnStatusChanged;
		}

		private void Unsubscribe(CiscoParticipant participant)
		{
			participant.OnStatusChanged -= ParticipantOnStatusChanged;
		}

		private void ParticipantOnStatusChanged(object sender, ParticipantStatusEventArgs args)
		{
			Status = args.Data.ToConferenceStatus();
		}

		#endregion
	}
}