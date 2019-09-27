using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;
using ICD.Connect.Conferencing.Zoom.Responses;

namespace ICD.Connect.Conferencing.Zoom.Components.Call
{
	public sealed class ZoomParticipant : AbstractWebParticipant
	{
		private readonly ZoomRoom m_ZoomRoom;

		public string UserId { get; private set; }
		public string AvatarUrl { get; private set; }
		public bool CanRecord { get; private set; }
		public bool IsRecording { get; private set; }

		public ZoomParticipant(ZoomRoom zoomRoom, ParticipantInfo info)
		{
			m_ZoomRoom = zoomRoom;
			UserId = info.UserId;
			Start = IcdEnvironment.GetLocalTime();
			Update(info);
		}

		#region Methods

		public void Update(ParticipantInfo info)
		{
			Name = info.UserName;
			Status = eParticipantStatus.Connected;
			SourceType = info.IsSendingVideo ? eCallType.Video : eCallType.Audio;
			IsMuted = info.AudioState == eAudioState.AUDIO_MUTED;
			IsHost = info.IsHost;
			IsSelf = info.IsMyself;
			AvatarUrl = info.AvatarUrl;

			//ParticipantInfo Doesn't track recording information if the Participant is the host.
			if (IsHost)
			{
				CanRecord = true;
				IsRecording = m_ZoomRoom.Components.GetComponent<CallComponent>().CallRecord;
			}
			else
			{
				CanRecord = info.CanRecord;
				IsRecording = info.IsRecording;
			}
		}

		public override void Kick()
		{
			m_ZoomRoom.SendCommand("zCommand Call Expel Id: {0}", UserId);
		}

		public override void Mute(bool mute)
		{
			m_ZoomRoom.SendCommand("zCommand Call MuteParticipant mute: {0} Id: {1}", 
				mute ? "on" : "off", 
				UserId);
		}

		public void AllowParticipantRecord(bool enabled)
		{
			m_ZoomRoom.Log(eSeverity.Debug, "Participant with userId: {0} Call Record Enable set to: {1}", UserId,
			               enabled);
			m_ZoomRoom.SendCommand("zCommand Call AllowRecord Id: {0} Enable: {1}", UserId, enabled ? "on" : "off");
		}

		#endregion

		#region Private Methods

		// internal only cause zoom sucks and this is the easiest way
		// to set the correct host state when host changes
		internal void SetIsHost(bool isHost)
		{
			IsHost = isHost;
		}

		#endregion

		#region Console

		public override string ConsoleHelp { get { return "Zoom conference participant"; } }

		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("User ID", UserId);
			addRow("Avatar URL", AvatarUrl);
			addRow("Can Record", CanRecord);
			addRow("Is Recording", IsRecording);
		}

		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in base.GetConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<bool>("AllowRecord",
			                                             "Allows a participant to record <true/false>",
			                                             b => AllowParticipantRecord(b));
		}

		#endregion
	}
}