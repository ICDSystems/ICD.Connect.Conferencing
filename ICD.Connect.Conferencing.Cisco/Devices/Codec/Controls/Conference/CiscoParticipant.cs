using System;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.Cameras;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Cameras;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Dialing;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;
using ICD.Connect.Conferencing.Participants.Enums;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Controls.Conference
{
	public sealed class CiscoParticipant : AbstractParticipant
	{
		private readonly CallStatus m_CallStatus;
		private readonly DialingComponent m_DialingComponent;

		private FarCamera m_CachedCamera;

		public override IRemoteCamera Camera
		{
			get
			{
				if (CallType != eCallType.Video)
					return null;
				return m_CachedCamera ?? (m_CachedCamera = new FarCamera(m_CallStatus.CallId, m_DialingComponent.Codec));
			}
		}

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="dialingComponent"></param>
		public CiscoParticipant([NotNull] CallStatus source, DialingComponent dialingComponent)
		{
			if (source == null)
				throw new ArgumentNullException("source");

			m_CallStatus = source;
			Subscribe(m_CallStatus);

			m_DialingComponent = dialingComponent;
			Subscribe(m_DialingComponent.Codec);

			InitializeValues();
		}

		private void InitializeValues()
		{
			CallType = m_CallStatus.CiscoCallType.ToCallType();
			AnswerState = m_CallStatus.AnswerState;
			Number = m_CallStatus.Number;
			Name = m_CallStatus.Name;
			Direction = m_CallStatus.Direction;
			Status = m_CallStatus.Status;
			DialTime = IcdEnvironment.GetUtcTime();
			StartTime = IcdEnvironment.GetUtcTime();
			EndTime = null;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal()
		{
			Unsubscribe(m_DialingComponent.Codec);
			Unsubscribe(m_CallStatus);

			base.DisposeFinal();
		}

		public override void Hold()
		{
			m_DialingComponent.Hold(m_CallStatus);
		}

		public override void Resume()
		{
			m_DialingComponent.Resume(m_CallStatus);
		}

		public override void Hangup()
		{
			m_DialingComponent.Hangup(m_CallStatus);
		}

		public override void SendDtmf(string data)
		{
			m_DialingComponent.SendDtmf(m_CallStatus, data);
		}

		public override void Kick()
		{
			throw new NotSupportedException();
		}

		public override void Mute(bool mute)
		{
			throw new NotSupportedException();
		}

		public override void ToggleHandRaise()
		{
			throw new NotSupportedException();
		}

		public override void RecordCallAction(bool stop)
		{
			throw new NotSupportedException();
		}

		#endregion

		#region Call Status Callbacks

		private void Subscribe([NotNull] CallStatus callStatus)
		{
			if (callStatus == null)
				throw new ArgumentNullException("callStatus");

			callStatus.OnCiscoCallTypeChanged += CallStatusOnCiscoCallTypeChanged;
			callStatus.OnAnswerStateChanged += CallStatusOnAnswerStateChanged;
			callStatus.OnNumberChanged += CallStatusOnNumberChanged;
			callStatus.OnNameChanged += CallStatusOnNameChanged;
			callStatus.OnDirectionChanged += CallStatusOnDirectionChanged;
			callStatus.OnDurationChanged += CallStatusOnDurationChanged;
			callStatus.OnStatusChanged += CallStatusOnStatusChanged;
		}

		private void Unsubscribe([NotNull] CallStatus callStatus)
		{
			if (callStatus == null)
				throw new ArgumentNullException("callStatus");

			callStatus.OnCiscoCallTypeChanged -= CallStatusOnCiscoCallTypeChanged;
			callStatus.OnAnswerStateChanged -= CallStatusOnAnswerStateChanged;
			callStatus.OnNumberChanged -= CallStatusOnNumberChanged;
			callStatus.OnNameChanged -= CallStatusOnNameChanged;
			callStatus.OnDirectionChanged -= CallStatusOnDirectionChanged;
			callStatus.OnDurationChanged -= CallStatusOnDurationChanged;
			callStatus.OnStatusChanged -= CallStatusOnStatusChanged;
		}

		private void CallStatusOnCiscoCallTypeChanged(object sender, GenericEventArgs<eCiscoCallType> args)
		{
			CallType = args.Data.ToCallType();
		}

		private void CallStatusOnAnswerStateChanged(object sender, GenericEventArgs<eCallAnswerState> args)
		{
			AnswerState = args.Data;
		}

		private void CallStatusOnNumberChanged(object sender, StringEventArgs args)
		{
			Number = args.Data;
		}

		private void CallStatusOnNameChanged(object sender, StringEventArgs args)
		{
			Name = args.Data;
		}

		private void CallStatusOnDirectionChanged(object sender, GenericEventArgs<eCallDirection> args)
		{
			Direction = args.Data;
		}

		private void CallStatusOnDurationChanged(object sender, IntEventArgs args)
		{
			DateTime end = EndTime ?? IcdEnvironment.GetUtcTime();
			StartTime = end - TimeSpan.FromSeconds(args.Data);
		}

		private void CallStatusOnStatusChanged(object sender, GenericEventArgs<eParticipantStatus> args)
		{
			Status = args.Data;
			if (!args.Data.GetIsOnline())
				EndTime = IcdEnvironment.GetUtcTime();
		}

		#endregion

		#region Codec Callbacks

		private void Subscribe(CiscoCodecDevice codec)
		{
			if (codec == null)
				return;

			codec.OnConnectedStateChanged += CodecOnConnectedStateChanged;
		}

		private void Unsubscribe(CiscoCodecDevice codec)
		{
			if (codec == null)
				return;

			codec.OnConnectedStateChanged -= CodecOnConnectedStateChanged;
		}

		private void CodecOnConnectedStateChanged(object sender, BoolEventArgs args)
		{
			if (!args.Data)
				Status = eParticipantStatus.Disconnected;
		}

		#endregion
	}
}