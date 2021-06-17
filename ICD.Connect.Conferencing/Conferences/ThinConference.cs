using System;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Participants;

namespace ICD.Connect.Conferencing.Conferences
{
	#region Delegates

	public delegate void ThinConferenceActionCallback(ThinConference sender);
	public delegate void ThinConferenceDtmfCallback(ThinConference sender, string data);

	#endregion

	public sealed class ThinConference : AbstractConference<ThinParticipant>, IDisposable
	{
		#region Fields

		private ThinConferenceDtmfCallback m_DtmfCallback;
		private ThinConferenceActionCallback m_LeaveConferenceCallback;
		private ThinConferenceActionCallback m_EndConferenceCallback;
		private ThinConferenceActionCallback m_StartRecordingCallback;
		private ThinConferenceActionCallback m_StopRecordingCallback;
		private ThinConferenceActionCallback m_PauseRecordingCallback;
		private ThinConferenceActionCallback m_HoldCallback;
		private ThinConferenceActionCallback m_ResumeCallback;

		#endregion

		#region Properties

		public ThinConferenceActionCallback LeaveConferenceCallback
		{
			get { return m_LeaveConferenceCallback; }
			set
			{
				m_LeaveConferenceCallback = value;

				SupportedConferenceFeatures = SupportedConferenceFeatures.SetFlags(eConferenceFeatures.LeaveConference,
				                                                                   m_LeaveConferenceCallback != null);
			}
		}

		public ThinConferenceActionCallback EndConferenceCallback
		{
			get { return m_EndConferenceCallback; }
			set
			{
				m_EndConferenceCallback = value;

				SupportedConferenceFeatures = SupportedConferenceFeatures.SetFlags(eConferenceFeatures.EndConference,
				                                                                   m_EndConferenceCallback != null);
			}
		}

		public ThinConferenceActionCallback StartRecordingCallback
		{
			get { return m_StartRecordingCallback; }
			set
			{
				m_StartRecordingCallback = value;
				SupportedConferenceFeatures = SupportedConferenceFeatures.SetFlags(eConferenceFeatures.StartRecording,
				                                                                   m_StartRecordingCallback != null);
			}
		}

		public ThinConferenceActionCallback StopRecordingCallback
		{
			get { return m_StopRecordingCallback; }
			set
			{
				m_StopRecordingCallback = value;
				SupportedConferenceFeatures = SupportedConferenceFeatures.SetFlags(eConferenceFeatures.StopRecording,
																				   m_StopRecordingCallback != null);
			}
		}

		public ThinConferenceActionCallback PauseRecordingCallback
		{
			get { return m_PauseRecordingCallback; }
			set
			{
				m_PauseRecordingCallback = value;
				SupportedConferenceFeatures = SupportedConferenceFeatures.SetFlags(eConferenceFeatures.PauseRecording,
																				   m_PauseRecordingCallback != null);
			}
		}

		public ThinConferenceActionCallback HoldCallback
		{
			get { return m_HoldCallback; }
			set
			{
				m_HoldCallback = value;
				UpdateConferenceHoldFeature();
			}
		}

		public ThinConferenceActionCallback ResumeCallback
		{
			get { return m_ResumeCallback; }
			set
			{
				m_ResumeCallback = value;
				UpdateConferenceHoldFeature();
			}
		}

		public ThinConferenceDtmfCallback DtmfCallback
		{
			get { return m_DtmfCallback; }
			set
			{
				m_DtmfCallback = value;

				SupportedConferenceFeatures = SupportedConferenceFeatures.SetFlags(eConferenceFeatures.SendDtmf, m_DtmfCallback != null);
			}
		}

		#endregion

		#region Public Methods

		protected override void DisposeFinal()
		{
			LeaveConferenceCallback = null;
			EndConferenceCallback = null;
			StartRecordingCallback = null;
			StopRecordingCallback = null;
			PauseRecordingCallback = null;
			HoldCallback = null;
			ResumeCallback = null;
			DtmfCallback = null;

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

		/// <summary>
		/// Holds the conference
		/// </summary>
		public override void Hold()
		{
			var handler = HoldCallback;
			if (handler != null)
				handler(this);
		}

		/// <summary>
		/// Resumes the conference
		/// </summary>
		public override void Resume()
		{
			var handler = ResumeCallback;
			if (handler != null)
				handler(this);
		}

		/// <summary>
		/// Sends DTMF to the participant.
		/// </summary>
		/// <param name="data"></param>
		public override void SendDtmf(string data)
		{
			var handler = DtmfCallback;
			if (handler != null)
				handler(this, data);
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

		#endregion

		#region Private Methods

		private void UpdateConferenceHoldFeature()
		{
			bool holdSupported = HoldCallback != null && ResumeCallback != null;
			SupportedConferenceFeatures = SupportedConferenceFeatures.SetFlags(eConferenceFeatures.Hold, holdSupported);
		}

		#endregion
	}
}
