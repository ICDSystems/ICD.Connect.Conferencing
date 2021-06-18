using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Cameras;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.Call;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.Camera;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Controls.Camera;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Controls.Conferencing
{
	public sealed class ZoomParticipant : AbstractParticipant
	{
		private readonly CallComponent m_CallComponent;

		private FarEndZoomCamera m_FarEndCamera;

		#region Properties

		public string UserId { get; private set; }
		public string AvatarUrl { get; private set; }

		public override IRemoteCamera Camera
		{
			get
			{
				string user = IsSelf ? "0" : UserId;
				CameraComponent cameraComponent = m_CallComponent.Parent.Components.GetComponent<CameraComponent>();

				return m_FarEndCamera ?? (m_FarEndCamera = new FarEndZoomCamera(cameraComponent, user));
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="callComponent"></param>
		/// <param name="info"></param>
		public ZoomParticipant(CallComponent callComponent, ParticipantInfo info)
		{
			if (callComponent == null)
				throw new ArgumentNullException("callComponent");

			m_CallComponent = callComponent;

			SupportedParticipantFeatures = eParticipantFeatures.GetCamera |
			                               eParticipantFeatures.GetIsMuted |
			                               eParticipantFeatures.GetIsSelf |
			                               eParticipantFeatures.GetIsHost |
			                               eParticipantFeatures.Kick |
			                               eParticipantFeatures.SetMute;

			UserId = info.UserId;
			StartTime = IcdEnvironment.GetUtcTime();
			Update(info);
		}

		#region Methods

		public void Update(ParticipantInfo info)
		{
			Name = info.UserName;
			Status = eParticipantStatus.Connected;
			CallType = info.IsSendingVideo ? eCallType.Video : eCallType.Audio;
			IsMuted = info.AudioState == eAudioState.AUDIO_MUTED;
			IsHost = info.IsHost;
			IsSelf = info.IsMyself;
			AvatarUrl = info.AvatarUrl;
		}

		public void AllowParticipantRecord(bool enabled)
		{
			m_CallComponent.AllowParticipantRecord(UserId, enabled);
		}

		public override void Admit()
		{
			throw new NotSupportedException();
		}

		public override void Kick()
		{
			m_CallComponent.ExpelParticipant(UserId);
		}

		public override void Mute(bool mute)
		{
			m_CallComponent.MuteParticipant(UserId, mute);
		}

		public override void SetHandPosition(bool raised)
		{
			throw new NotSupportedException();
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
		}

		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<bool>("AllowRecord",
														 "Allows a participant to record <true/false>",
														 b => AllowParticipantRecord(b));
		}

		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}