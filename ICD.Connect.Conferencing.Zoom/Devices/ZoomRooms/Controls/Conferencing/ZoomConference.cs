using System;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.Call;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Controls.Conferencing
{
	public sealed class ZoomConference : AbstractConference<ZoomParticipant>
	{
		private readonly CallComponent m_CallComponent;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="callComponent"></param>
		public ZoomConference([NotNull] CallComponent callComponent)
		{
			if (callComponent == null)
				throw new ArgumentNullException("callComponent");

			SupportedConferenceFeatures = eConferenceFeatures.LeaveConference |
			                              eConferenceFeatures.EndConference |
			                              eConferenceFeatures.StartRecording |
			                              eConferenceFeatures.StopRecording;

			m_CallComponent = callComponent;
			Subscribe(m_CallComponent);
		}

		#region Methods

		protected override void DisposeFinal()
		{
			Unsubscribe(m_CallComponent);

			base.DisposeFinal();
		}

		/// <summary>
		/// Leaves the conference, keeping the conference in tact for other participants.
		/// </summary>
		public override void LeaveConference()
		{
			Status = eConferenceStatus.Disconnecting;
			m_CallComponent.CallLeave();
		}

		/// <summary>
		/// Ends the conference for all participants.
		/// </summary>
		public override void EndConference()
		{
			Status = eConferenceStatus.Disconnecting;
			m_CallComponent.CallDisconnect();
		}

		public override void Hold()
		{
			throw new NotSupportedException();
		}

		public override void Resume()
		{
			throw new NotSupportedException();
		}

		public override void SendDtmf(string data)
		{
			throw new NotSupportedException();
		}

		public override void StartRecordingConference()
		{
			m_CallComponent.EnableCallRecord(true);
		}

		public override void StopRecordingConference()
		{
			m_CallComponent.EnableCallRecord(false);
		}

		public override void PauseRecordingConference()
		{
			throw new NotSupportedException();
		}

		#endregion

		#region CallComponent Callbacks

		/// <summary>
		/// Subscribe to the call component events.
		/// </summary>
		/// <param name="callComponent"></param>
		private void Subscribe(CallComponent callComponent)
		{
			callComponent.OnStatusChanged += CallComponentOnStatusChanged;
			callComponent.OnParticipantAdded += CallComponentOnParticipantAdded;
			callComponent.OnParticipantUpdated += CallComponentOnParticipantUpdated;
			callComponent.OnParticipantRemoved += CallComponentOnParticipantRemoved;
			callComponent.OnNeedWaitForHost += CallComponentOnNeedWaitForHost;
			callComponent.OnCallRecordChanged += CallComponentOnCallRecordChanged;
			callComponent.OnCanRecordChanged += CallComponentOnCanRecordChanged;
		}

		/// <summary>
		/// Subscribe to the call component events.
		/// </summary>
		/// <param name="callComponent"></param>
		private void Unsubscribe(CallComponent callComponent)
		{
			callComponent.OnStatusChanged -= CallComponentOnStatusChanged;
			callComponent.OnParticipantAdded -= CallComponentOnParticipantAdded;
			callComponent.OnParticipantUpdated -= CallComponentOnParticipantUpdated;
			callComponent.OnParticipantRemoved -= CallComponentOnParticipantRemoved;
			callComponent.OnNeedWaitForHost -= CallComponentOnNeedWaitForHost;
			callComponent.OnCallRecordChanged -= CallComponentOnCallRecordChanged;
			callComponent.OnCanRecordChanged -= CallComponentOnCanRecordChanged;
		}

		/// <summary>
		/// Called when a participant is added to the room.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void CallComponentOnParticipantRemoved(object sender, GenericEventArgs<ParticipantInfo> eventArgs)
		{
			ZoomParticipant participant = GetParticipants().FirstOrDefault(p => p.UserId == eventArgs.Data.UserId);

			if (participant == null)
				return;

			participant.Update(eventArgs.Data);
			RemoveParticipant(participant);
		}

		/// <summary>
		/// Called when a participant is added to the room.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void CallComponentOnParticipantAdded(object sender, GenericEventArgs<ParticipantInfo> eventArgs)
		{
			ZoomParticipant participant = GetParticipants().FirstOrDefault(p => p.UserId == eventArgs.Data.UserId);

			if (participant == null)
			{
				participant = new ZoomParticipant(m_CallComponent, eventArgs.Data);
				AddParticipant(participant);
			}
			else
				participant.Update(eventArgs.Data);
		}

		/// <summary>
		/// Called when a participant is updated in the room.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void CallComponentOnParticipantUpdated(object sender, GenericEventArgs<ParticipantInfo> eventArgs)
		{
			ZoomParticipant participant = GetParticipants().FirstOrDefault(p => p.UserId == eventArgs.Data.UserId);

			if (participant == null)
			{
				participant = new ZoomParticipant(m_CallComponent, eventArgs.Data);
				AddParticipant(participant);
			}
			else
				participant.Update(eventArgs.Data);
		}

		/// <summary>
		/// Called when the conference status changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void CallComponentOnStatusChanged(object sender, GenericEventArgs<eCallStatus> eventArgs)
		{
			switch (eventArgs.Data)
			{
				case eCallStatus.CONNECTING_MEETING:
					Status = eConferenceStatus.Connecting;
					break;

				case eCallStatus.IN_MEETING:
					Status = eConferenceStatus.Connected;
					StartTime = IcdEnvironment.GetUtcTime();
					break;

				case eCallStatus.NOT_IN_MEETING:
				case eCallStatus.LOGGED_OUT:
					ClearParticipants();
					Status = eConferenceStatus.Disconnected;
					EndTime = IcdEnvironment.GetUtcTime();
					break;

				case eCallStatus.UNKNOWN:
					Status = eConferenceStatus.Undefined;
					break;
			}
		}

		/// <summary>
		/// Zoom doesn't give connection feedback upon joining a call lobby,
		/// but we do get feedback when we need to wait for the host to start the call.
		/// So when we are waiting for the host in the lobby change the conference status to connected.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void CallComponentOnNeedWaitForHost(object sender, BoolEventArgs e)
		{
			if (e.Data)
				Status = eConferenceStatus.Connected;
		}

		private void CallComponentOnCallRecordChanged(object sender, BoolEventArgs e)
		{
			RecordingStatus = e.Data ? eConferenceRecordingStatus.Recording : eConferenceRecordingStatus.Stopped;
		}

		private void CallComponentOnCanRecordChanged(object sender, BoolEventArgs args)
		{
			SupportedConferenceFeatures =
				SupportedConferenceFeatures.SetFlags(
                         eConferenceFeatures.StartRecording |
                         eConferenceFeatures.StopRecording,
                         args.Data);
		}

		#endregion

		#region Console

		public override string ConsoleHelp { get { return "Zoom Room Conference"; } }

		#endregion
	}
}
