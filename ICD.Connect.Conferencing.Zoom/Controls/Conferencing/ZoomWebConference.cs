using System;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.Zoom.Components.Call;
using ICD.Connect.Conferencing.Zoom.Responses;

namespace ICD.Connect.Conferencing.Zoom.Controls.Conferencing
{
	public sealed class ZoomWebConference : AbstractWebConference
	{
		private readonly CallComponent m_CallComponent;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="callComponent"></param>
		public ZoomWebConference(CallComponent callComponent)
		{
			if (callComponent == null)
				throw new ArgumentNullException("callComponent");

			m_CallComponent = callComponent;
			Subscribe(m_CallComponent);
		}

		#region Methods

		/// <summary>
		/// Leaves the conference, keeping the conference in tact for other participants.
		/// </summary>
		public override void LeaveConference()
		{
			m_CallComponent.CallLeave();
		}

		/// <summary>
		/// Ends the conference for all participants.
		/// </summary>
		public override void EndConference()
		{
			m_CallComponent.CallDisconnect();
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
			callComponent.OnParticipantRemoved += CallComponentOnParticipantRemoved;
		}

		/// <summary>
		/// Called when a participant is added to the room.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void CallComponentOnParticipantRemoved(object sender, GenericEventArgs<ParticipantInfo> eventArgs)
		{
			ZoomWebParticipant participant;
			bool found = GetParticipants()
				.Cast<ZoomWebParticipant>()
				.TryFirst(p => p.UserId == eventArgs.Data.UserId, out participant);


			if (found)
				RemoveParticipant(participant);
		}

		/// <summary>
		/// Called when a participant is removed from the room.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void CallComponentOnParticipantAdded(object sender, GenericEventArgs<ParticipantInfo> eventArgs)
		{
			ZoomWebParticipant participant =
				GetParticipants()
					.Cast<ZoomWebParticipant>()
					.FirstOrDefault(p => p.UserId == eventArgs.Data.UserId);

			if (participant == null)
			{
				participant = new ZoomWebParticipant(m_CallComponent.Parent, eventArgs.Data);
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
					Start = IcdEnvironment.GetLocalTime();
					break;

				case eCallStatus.NOT_IN_MEETING:
				case eCallStatus.LOGGED_OUT:
					Clear();
					Status = eConferenceStatus.Disconnected;
					break;

				case eCallStatus.UNKNOWN:
					Status = eConferenceStatus.Undefined;
					break;
			}
		}

		#endregion
	}
}
