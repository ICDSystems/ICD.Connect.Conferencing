using System;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.Participants
{
	public delegate void ThinWebParticipantKickCallback(ThinWebParticipant sender);

	public delegate void ThinWebParticipantMuteCallback(ThinWebParticipant sender, bool mute);

	public sealed class ThinWebParticipant : AbstractWebParticipant
	{
		public ThinWebParticipantKickCallback KickCallback { get; set; }

		public ThinWebParticipantMuteCallback MuteCallback { get; set; }
		
		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal()
		{
			base.DisposeFinal();

			KickCallback = null;
			MuteCallback = null;
		}

		#region Methods

		/// <summary>
		/// Disconnects the source.
		/// </summary>
		public override void Kick()
		{
			ThinWebParticipantKickCallback handler = KickCallback;
			if (handler != null)
				handler(this);
		}

		/// <summary>
		/// Disconnects the source.
		/// </summary>
		public override void Mute(bool mute)
		{
			ThinWebParticipantMuteCallback handler = MuteCallback;
			if (handler != null)
				handler(this, mute);
		}

		public void SetName(string name)
		{
			Name = name;
		}

		public void SetSourceType(eCallType sourceType)
		{
			SourceType = sourceType;
		}

		public void SetIsMuted(bool isMuted)
		{
			IsMuted = isMuted;
		}

		public void SetIsHost(bool isHost)
		{
			IsHost = isHost;
		}

		public void SetIsSelf(bool isSelf)
		{
			IsSelf = isSelf;
		}

		public void SetStatus(eParticipantStatus status)
		{
			Status = status;
		}

		public void SetStart(DateTime start)
		{
			Start = start;
		}

		public void SetEnd(DateTime end)
		{
			End = end;
		}

		#endregion
	}
}
