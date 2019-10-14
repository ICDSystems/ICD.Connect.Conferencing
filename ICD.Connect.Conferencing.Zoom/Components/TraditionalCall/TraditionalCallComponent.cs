using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;
using ICD.Connect.Conferencing.Zoom.Responses;

namespace ICD.Connect.Conferencing.Zoom.Components.TraditionalCall
{
	public sealed class TraditionalCallComponent : AbstractZoomRoomComponent, ITraditionalConference
	{
		#region Events

		/// <summary>
		/// Raised when a participant is added to the conference.
		/// </summary>
		public event EventHandler<ParticipantEventArgs> OnParticipantAdded;

		/// <summary>
		/// Raised when a participant is removed from the conference.
		/// </summary>
		public event EventHandler<ParticipantEventArgs> OnParticipantRemoved;

		/// <summary>
		/// Raised when the conference status changes.
		/// </summary>
		public event EventHandler<ConferenceStatusEventArgs> OnStatusChanged;

		/// <summary>
		/// Raised when the Call Id changes.
		/// </summary>
		public event EventHandler<StringEventArgs> OnCallIdChanged; 

		#endregion

		private eConferenceStatus m_Status;
		private string m_CallId;

		private readonly TraditionalZoomPhoneCallInfo m_CallInfo;
		private TraditionalZoomParticipant m_Participant;

		#region Properties

		public string CallId
		{
			get { return m_CallId; }
			private set
			{
				if (value == m_CallId)
					return;

				m_CallId = value;
				Parent.Log(eSeverity.Informational, "Call ID set to: {0}", m_CallId);
				OnCallIdChanged.Raise(this, new StringEventArgs(m_CallId));
			}
		}

		public eConferenceStatus Status
		{
			get { return m_Status; }
			private set
			{
				if (value == m_Status)
					return;

				m_Status = value;
				Parent.Log(eSeverity.Informational, "Traditional Zoom Call status changed to: {0}",
				           StringUtils.NiceName(m_Status));
				OnStatusChanged.Raise(this, new ConferenceStatusEventArgs(m_Status));
			}
		}

		public DateTime? Start { get; private set; }

		public DateTime? End { get; private set; }

		public eCallType CallType { get { return eCallType.Audio; } }

		#endregion

		#region Constructor

		public TraditionalCallComponent(ZoomRoom parent) : base(parent)
		{
			Subscribe(Parent);
			m_CallInfo = new TraditionalZoomPhoneCallInfo();
		}

		protected override void DisposeFinal()
		{
			base.DisposeFinal();

			OnParticipantAdded = null;
			OnParticipantRemoved = null;
			OnStatusChanged = null;
			OnCallIdChanged = null;

			Unsubscribe(Parent);
		}

		#endregion

		#region Methods

		IEnumerable<ITraditionalParticipant> IConference<ITraditionalParticipant>.GetParticipants()
		{
			return GetParticipants();
		}

		IEnumerable<IParticipant> IConference.GetParticipants()
		{
			return GetParticipants();
		}

		#endregion

		#region Private Methods

		private IEnumerable<ITraditionalParticipant> GetParticipants()
		{
			yield return m_Participant;
		}

		private void AddParticipant(TraditionalZoomParticipant participant)
		{
			if (CallId == participant.CallId)
				participant.Update(m_CallInfo);
			else
			{
				CallId = participant.CallId;
				m_Participant = participant;
				Parent.Log(eSeverity.Informational, "Adding New Participant: {0}", participant.Name);
				OnParticipantAdded.Raise(this, new ParticipantEventArgs(m_Participant));
			}

			switch (m_CallInfo.Status)
			{
				case eZoomPhoneCallStatus.Ringing:
				case eZoomPhoneCallStatus.Init:
					Status = eConferenceStatus.Connecting;
					break;

				case eZoomPhoneCallStatus.InCall:
					Status = eConferenceStatus.Connected;
					break;

				case eZoomPhoneCallStatus.Incoming:
				case eZoomPhoneCallStatus.NotFound:
				case eZoomPhoneCallStatus.None:
					Status = eConferenceStatus.Undefined;
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void RemoveParticipant(TraditionalZoomParticipant participant)
		{
			if (CallId != participant.CallId)
				return;

			CallId = null;
			m_Participant = null;
			Status = eConferenceStatus.Disconnected;
			Parent.Log(eSeverity.Informational, "Removing Participant: {0} Reason: {1}", m_CallInfo.PeerDisplayName,
			           m_CallInfo.Reason);
			OnParticipantRemoved.Raise(this, new ParticipantEventArgs(participant));
		}

		#endregion

		#region Parent Callbacks

		private void Subscribe(ZoomRoom parent)
		{
			Parent.RegisterResponseCallback<PhoneCallStatusResponse>(PhoneCallStatusResponseCallback);
			Parent.RegisterResponseCallback<PhoneCallTerminatedResponse>(PhoneCallTerminatedResponse);
		}

		private void Unsubscribe(ZoomRoom parent)
		{
			Parent.UnregisterResponseCallback<PhoneCallStatusResponse>(PhoneCallStatusResponseCallback);
			Parent.UnregisterResponseCallback<PhoneCallTerminatedResponse>(PhoneCallTerminatedResponse);
		}

		private void PhoneCallStatusResponseCallback(ZoomRoom zoomroom, PhoneCallStatusResponse response)
		{
			var data = response.PhoneCallStatus;
			if (data == null)
				return;
			
			m_CallInfo.UpdateStatusInfo(data);

			var participant = new TraditionalZoomParticipant(Parent, m_CallInfo);
			AddParticipant(participant);
		}

		private void PhoneCallTerminatedResponse(ZoomRoom zoomroom, PhoneCallTerminatedResponse response)
		{
			var data = response.PhoneCallTerminated;
			if (data == null)
				return;

			m_CallInfo.UpdateTerminateInfo(data);

			var participant =
				GetParticipants().FirstOrDefault(p => p.Number == data.PeerNumber) as TraditionalZoomParticipant;
			RemoveParticipant(participant);
		}

		#endregion

		#region Console

		public override string ConsoleHelp { get { return "Zoom Room Traditional Call"; } }

		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			foreach (var participant in GetParticipants())
				if (participant != null)
					yield return participant;
		}

		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("CallID", CallId);
			addRow("Status", Status);
		}

		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			return GetBaseConsoleCommands();
		}

		private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
		{
			return base.GetConsoleNodes();
		}

		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
