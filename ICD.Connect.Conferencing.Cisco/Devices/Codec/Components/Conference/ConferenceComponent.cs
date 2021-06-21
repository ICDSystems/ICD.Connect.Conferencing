using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Xml;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Conference
{
	public enum eCallRecordingStatus
	{
		/// <summary>
		/// Recording is not available.
		/// </summary>
		None = 0,

		/// <summary>
		/// The recording is ongoing.
		/// </summary>
		Recording = 1,

		/// <summary>
		/// The recording is paused.
		/// </summary>
		Paused = 2
	}

	public static class CallRecordingStatusExtensions
	{
		/// <summary>
		/// Can only start if no recording is currently ongoing or paused.
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static bool CanStart(this eCallRecordingStatus extends)
		{
			return extends == eCallRecordingStatus.None;
		}

		/// <summary>
		/// Can only stop if there is an ongoing or paused recording.
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static bool CanStop(this eCallRecordingStatus extends)
		{
			return extends != eCallRecordingStatus.None;
		}

		/// <summary>
		/// Can only pause if there is an ongoing recording.
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static bool CanPause(this eCallRecordingStatus extends)
		{
			return extends == eCallRecordingStatus.Recording;
		}

		/// <summary>
		/// Can only resume if there is a paused recording.
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static bool CanResume(this eCallRecordingStatus extends)
		{
			return extends == eCallRecordingStatus.Paused;
		}
	}

	public sealed class ConferenceComponent : AbstractCiscoComponent
	{
		#region Events

		public event EventHandler<GenericEventArgs<eCallRecordingStatus>> OnCallRecordingStatusChanged;
		public event EventHandler<GenericEventArgs<WebexParticipantInfo>> OnWebexParticipantListUpdated;
		public event EventHandler<GenericEventArgs<IEnumerable<WebexParticipantInfo>>> OnWebexParticipantsListSearchResult;

		#endregion

		#region Fields

		private eCallRecordingStatus m_CallRecordingStatus;

		#endregion

		#region Properties

		/// <summary>
		/// The current call recording state.
		/// </summary>
		public eCallRecordingStatus CallRecordingStatus
		{
			get { return m_CallRecordingStatus; }
			private set
			{
				if (m_CallRecordingStatus == value)
					return;

				m_CallRecordingStatus = value;
				Codec.Logger.Log(eSeverity.Informational, "Call recording status set to: {0}", m_CallRecordingStatus);
				OnCallRecordingStatusChanged.Raise(this, m_CallRecordingStatus);
			}
		}

		#endregion

		#region Constructor

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="codec"></param>
		public ConferenceComponent(CiscoCodecDevice codec)
			: base(codec)
		{
			Subscribe(codec);
		}

		#endregion

		#region Methods

		/// <summary>
		/// Sends the raise hand command.
		/// </summary>
		/// <param name="callId"></param>
		public void RaiseHand(int callId)
		{
			Codec.SendCommand("xCommand Conference Hand Raise CallId: {0}", callId);
			Codec.Logger.Log(eSeverity.Informational, "Raising Hand for call {0}", callId);
		}

		/// <summary>
		/// Sends the lower hand command.
		/// </summary>
		/// <param name="callId"></param>
		public void LowerHand(int callId)
		{
			Codec.SendCommand("xCommand Conference Hand Lower CallId: {0}", callId);
			Codec.Logger.Log(eSeverity.Informational, "Lowering Hand for call {0}", callId);
		}

		/// <summary>
		/// Starts recording the call.
		/// </summary>
		/// <param name="callId"></param>
		public void RecordingStart(int callId)
		{
			Codec.SendCommand("xCommand Conference Recording Start CallId: {0}", callId);
			Codec.Logger.Log(eSeverity.Informational, "Starting Recording for call {0}", callId);
		}

		/// <summary>
		/// Stops recording the call.
		/// </summary>
		/// <param name="callId"></param>
		public void RecordingStop(int callId)
		{
			Codec.SendCommand("XCommand Conference Recording Stop CallId: {0}", callId);
			Codec.Logger.Log(eSeverity.Informational, "Stopping Recording for call {0}", callId);
		}

		/// <summary>
		/// Pauses recording the call.
		/// </summary>
		/// <param name="callId"></param>
		public void RecordingPause(int callId)
		{
			Codec.SendCommand("xCommand Conference Recording Pause CallId: {0}", callId);
			Codec.Logger.Log(eSeverity.Informational, "Pausing Recording for call {0}", callId);
		}

		/// <summary>
		/// Resumes recording the call.
		/// </summary>
		/// <param name="callId"></param>
		public void RecordingResume(int callId)
		{
			Codec.SendCommand("xCommand Conference Recording Resume CallId: {0}", callId);
			Codec.Logger.Log(eSeverity.Informational, "Resuming Recording for call {0}", callId);
		}

		/// <summary>
		/// Builds a search query for participants in the call with the specified ID.
		/// </summary>
		/// <param name="callId"></param>
		/// <param name="limit"></param>
		/// <param name="offset"></param>
		/// <param name="search"></param>
		public void ParticipantListSearch(int callId, int? limit, int? offset, [CanBeNull] string search)
		{
			string limitString = limit.HasValue ? string.Format(" Limit: {0}", limit) : "";
			string offsetString = offset.HasValue ? string.Format(" Offset: {0}", offset) : "";
			string searchString = search != null ? string.Format(" SearchString: {0}", search) : "";

			Codec.SendCommand("xCommand Conference ParticipantList Search CallId: {0}{1}{2}{3}", callId, limitString, offsetString, searchString);
			Codec.Logger.Log(eSeverity.Informational, "Querying call: {0} participants", callId);
		}

		/// <summary>
		/// Admits the specified participant to the specified call.
		/// </summary>
		/// <param name="callId"></param>
		/// <param name="participantId"></param>
		public void ParticipantAdmit(int callId, string participantId)
		{
			Codec.SendCommand("xCommand Conference Participant Admit CallId: {0} ParticipantId: {1}", callId, participantId);
			Codec.Logger.Log(eSeverity.Informational, "Admitting participant with ID: {0} to call with ID: {1}", participantId, callId);
		}

		/// <summary>
		/// Disconnects the specified participant from the specified call.
		/// </summary>
		/// <param name="callId"></param>
		/// <param name="participantId"></param>
		public void ParticipantDisconnect(int callId, string participantId)
		{
			Codec.SendCommand("XCommand Conference Participant Disconnect CallId: {0} ParticipantId: {1}", callId, participantId);
			Codec.Logger.Log(eSeverity.Informational, "Disconnecting participant with ID: {0} from call with ID: {1}", participantId, callId);
		}

		/// <summary>
		/// Sets the mute state for the specified participant in the specified call.
		/// </summary>
		/// <param name="mute"></param>
		/// <param name="callId"></param>
		/// <param name="participantId"></param>
		public void ParticipantMute(bool mute, int callId, string participantId)
		{
			string muteString = mute ? "On" : "Off";

			Codec.SendCommand("xCommand Conference Participant Mute AudioMute: {0} CallId: {1} ParticipantId: {2}", muteString, callId, participantId);
			Codec.Logger.Log(eSeverity.Informational, "Setting participant with ID: {0} in call with ID: {1} Mute state to: {2}", participantId, callId, muteString);
		}

		/// <summary>
		/// Leaves a meeting in progress, allowing other participants to continue the meeting.
		/// If user is the host a new host is assigned automatically.
		/// </summary>
		/// <param name="callId"></param>
		public void TransferHostAndLeave(int callId)
        {
			Codec.SendCommand("xCommand Conference TransferHostAndLeave CallId: {0}", callId);
			Codec.Logger.Log(eSeverity.Informational, "Transferring host and leaving meeting with ID: {0}", callId);
        }

		#endregion

		#region Codec Callbacks

		/// <summary>
		/// Subscribes to the codec events.
		/// </summary>
		/// <param name="codec"></param>
		protected override void Subscribe(CiscoCodecDevice codec)
		{
			base.Subscribe(codec);

			codec.RegisterParserCallback(ParseCallRecordingStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "Conference",
			                             "Call", "Recording");
			codec.RegisterParserCallback(ParseParticipantsList, CiscoCodecDevice.XEVENT_ELEMENT, "Conference",
			                             "ParticipantList", "ParticipantUpdated");
			codec.RegisterParserCallback(ParseParticipantsListSearchresult, "ParticipantListSearchResult");
		}

		/// <summary>
		/// Unsubscribes from the codec events.
		/// </summary>
		/// <param name="codec"></param>
		protected override void Unsubscribe(CiscoCodecDevice codec)
		{
			base.Unsubscribe(codec);

			codec.UnregisterParserCallback(ParseCallRecordingStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "Conference",
			                               "Call", "Recording");
			codec.UnregisterParserCallback(ParseParticipantsList, CiscoCodecDevice.XEVENT_ELEMENT, "Conference",
			                             "ParticipantList", "ParticipantUpdated");
			codec.UnregisterParserCallback(ParseParticipantsListSearchresult, "ParticipantListSearchResult");
		}

		private void ParseCallRecordingStatus(CiscoCodecDevice codec, string resultid, string xml)
		{
			string content = XmlUtils.GetInnerXml(xml);
			CallRecordingStatus = EnumUtils.Parse<eCallRecordingStatus>(content, true);
		}

		private void ParseParticipantsList(CiscoCodecDevice codec, string resultid, string xml)
		{
			WebexParticipantInfo info = WebexParticipantInfo.FromXml(xml);
			OnWebexParticipantListUpdated.Raise(this, info);
		}

		private void ParseParticipantsListSearchresult(CiscoCodecDevice codec, string resultid, string xml)
		{
			var participantInfos = XmlUtils.ReadListFromXml<WebexParticipantInfo>(xml, "Participant",
			                                                                      s => WebexParticipantInfo.FromXml(s)).ToArray();

			string selfId = XmlUtils.ReadChildElementContentAsString(xml, "ParticipantSelf");
			foreach (WebexParticipantInfo info in participantInfos)
				info.IsSelf = info.ParticipantId == selfId;

			OnWebexParticipantsListSearchResult.Raise(this, participantInfos);
		}

		#endregion

		#region Console

		public override string ConsoleName { get { return "ConferenceComponent"; } }

		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Call Recording Status", CallRecordingStatus);
		}

		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<int>("RaiseHand", "RaiseHand {CallId}", i => RaiseHand(i));
			yield return new GenericConsoleCommand<int>("LowerHand", "LowerHand {CallId}", i => LowerHand(i));
		}

		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}