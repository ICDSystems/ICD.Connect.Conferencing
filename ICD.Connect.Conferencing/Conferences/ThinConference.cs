using System;
using ICD.Connect.Conferencing.Participants;

namespace ICD.Connect.Conferencing.Conferences
{
	public delegate void ThinConferenceLeaveConferenceCallback(ThinConference sender);
	public delegate void ThinConferenceEndConferenceCallback(ThinConference sender);
	public delegate void ThinConferenceStartRecordingCallback(ThinConference sender);
	public delegate void ThinConferenceStopRecordingCallback(ThinConference sender);
	public delegate void ThinConferencePauseRecordingCallback(ThinConference sender);

	public sealed class ThinConference : AbstractConference<IParticipant>, IDisposable
	{
		public ThinConferenceLeaveConferenceCallback LeaveConferenceCallback { get; set; }
		public ThinConferenceEndConferenceCallback EndConferenceCallback { get; set; }
		public ThinConferenceStartRecordingCallback StartRecordingCallback { get; set; }
		public ThinConferenceStopRecordingCallback StopRecordingCallback { get; set; }
		public ThinConferencePauseRecordingCallback PauseRecordingCallback { get; set; }

		protected override void DisposeFinal()
		{
			LeaveConferenceCallback = null;
			EndConferenceCallback = null;
			StartRecordingCallback = null;
			StopRecordingCallback = null;
			PauseRecordingCallback = null;

			base.DisposeFinal();
		}

		public override void LeaveConference()
		{
			var handler = LeaveConferenceCallback;
			if (handler != null)
				handler(this);
		}

		public override void EndConference()
		{
			var handler = EndConferenceCallback;
			if (handler != null)
				handler(this);
		}

		public override void StartRecordingConference()
		{
			var handler = StartRecordingCallback;
			if (handler != null)
				handler(this);
		}

		public override void StopRecordingConference()
		{
			var handler = StopRecordingCallback;
			if (handler != null)
				handler(this);
		}

		public override void PauseRecordingConference()
		{
			var handler = PauseRecordingCallback;
			if (handler != null)
				handler(this);
		}
	}
}
