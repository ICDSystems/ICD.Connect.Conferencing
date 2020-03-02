using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Cameras;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;
using ICD.Connect.Conferencing.Zoom.Components.Call;
using ICD.Connect.Conferencing.Zoom.Components.Camera;
using ICD.Connect.Conferencing.Zoom.Controls.Camera;
using ICD.Connect.Conferencing.Zoom.Responses;

namespace ICD.Connect.Conferencing.Zoom.Controls.Conferencing
{
	public sealed class ZoomWebParticipant : AbstractWebParticipant
	{
		/// <summary>
		/// Raised when the participant can record state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnCanRecordChanged;

		private readonly CallComponent m_CallComponent;

		private bool m_CanRecord;

		private FarEndZoomCamera m_FarEndCamera;

		public string UserId { get; private set; }
		public string AvatarUrl { get; private set; }

		public bool CanRecord
		{
			get { return m_CanRecord; }
			private set
			{
				if (value == m_CanRecord)
					return;

				m_CanRecord = value;

				OnCanRecordChanged.Raise(this, new BoolEventArgs(m_CanRecord));
			}
		}

		public bool IsRecording { get; private set; }

		public override IRemoteCamera Camera
		{
			get
			{
				string user = IsSelf ? "0" : UserId;
				CameraComponent cameraComponent = m_CallComponent.Parent.Components.GetComponent<CameraComponent>();

				return m_FarEndCamera ?? (m_FarEndCamera = new FarEndZoomCamera(cameraComponent, user));
			}
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="callComponent"></param>
		/// <param name="info"></param>
		public ZoomWebParticipant(CallComponent callComponent, ParticipantInfo info)
		{
			if (callComponent == null)
				throw new ArgumentNullException("callComponent");

			m_CallComponent = callComponent;

			UserId = info.UserId;
			Start = IcdEnvironment.GetLocalTime();
			Update(info);

			Subscribe(m_CallComponent);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal()
		{
			OnCanRecordChanged = null;

			base.DisposeFinal();

			Unsubscribe(m_CallComponent);
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

			// ParticipantInfo doesn't track recording information if the Participant is the host.
			if (IsHost)
			{
				CanRecord = true;
				IsRecording = m_CallComponent.CallRecord;
			}
			else
			{
				CanRecord = info.CanRecord;
				IsRecording = info.IsRecording;
			}
		}

		public override void Kick()
		{
			m_CallComponent.ExpelParticipant(UserId);
		}

		public override void Mute(bool mute)
		{
			m_CallComponent.MuteParticipant(UserId, mute);
		}

		public void AllowParticipantRecord(bool enabled)
		{
			m_CallComponent.AllowParticipantRecord(UserId, enabled);
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

		#region Call Component Callbacks

		private void Subscribe(CallComponent callComponent)
		{
			callComponent.OnCallRecordChanged += CallComponentOnCallRecordChanged;
		}

		private void Unsubscribe(CallComponent callComponent)
		{
			callComponent.OnCallRecordChanged -= CallComponentOnCallRecordChanged;
		}

		private void CallComponentOnCallRecordChanged(object sender, BoolEventArgs e)
		{
			if (IsHost)
				IsRecording = e.Data;
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