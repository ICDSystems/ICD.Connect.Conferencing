using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Audio.Utils;
using ICD.Connect.Audio.VolumePoints;
using ICD.Connect.Conferencing.Controls.Dialing;

namespace ICD.Connect.Conferencing.ConferenceManagers
{
	/// <summary>
	/// VolumePoints may be tied to DSP or Microphone mute for privacy.
	/// These VolumePoints may be registered with the conference manager to follow
	/// the current privacy mute setting.
	/// </summary>
	public sealed class ConferenceManagerVolumePoints
	{
		private readonly SafeCriticalSection m_PointsSection;
		private readonly Dictionary<IVolumePoint, VolumePointHelper> m_Points;

		private readonly ConferenceManager m_ConferenceManager;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="conferenceManager"></param>
		public ConferenceManagerVolumePoints([NotNull] ConferenceManager conferenceManager)
		{
			if (conferenceManager == null)
				throw new ArgumentNullException("conferenceManager");

			m_ConferenceManager = conferenceManager;

			m_PointsSection = new SafeCriticalSection();
			m_Points = new Dictionary<IVolumePoint, VolumePointHelper>();

			Subscribe(m_ConferenceManager);
		}

		#region Methods

		/// <summary>
		/// Deregisters all of the registered volume points.
		/// </summary>
		public void Clear()
		{
			m_PointsSection.Enter();

			try
			{
				foreach (IVolumePoint point in GetVolumePoints())
					DeregisterVolumePoint(point);
			}
			finally
			{
				m_PointsSection.Leave();
			}
		}

		/// <summary>
		/// Gets the registered volume points.
		/// </summary>
		/// <returns></returns>
		[NotNull]
		public IEnumerable<IVolumePoint> GetVolumePoints()
		{
			return m_PointsSection.Execute(() => m_Points.Keys.ToArray());
		}

		/// <summary>
		/// Registers the volume point.
		/// </summary>
		/// <param name="volumePoint"></param>
		public bool RegisterVolumePoint([NotNull] IVolumePoint volumePoint)
		{
			if (volumePoint == null)
				throw new ArgumentNullException("volumePoint");

			m_PointsSection.Enter();

			try
			{
				if (m_Points.ContainsKey(volumePoint))
					return false;

				VolumePointHelper helper = new VolumePointHelper {VolumePoint = volumePoint};
				m_Points.Add(volumePoint, helper);

				Subscribe(helper);
			}
			finally
			{
				m_PointsSection.Leave();
			}

			UpdateVolumePoint(volumePoint);
			return true;
		}

		/// <summary>
		/// Deregisters the volume point.
		/// </summary>
		/// <param name="volumePoint"></param>
		public bool DeregisterVolumePoint([NotNull] IVolumePoint volumePoint)
		{
			if (volumePoint == null)
				throw new ArgumentNullException("volumePoint");

			m_PointsSection.Enter();

			try
			{
				VolumePointHelper helper;
				if (!m_Points.TryGetValue(volumePoint, out helper))
					return false;

				m_Points.Remove(volumePoint);

				Unsubscribe(helper);

				return true;
			}
			finally
			{
				m_PointsSection.Leave();
			}
		}

		/// <summary>
		/// Updates the volume points to match the privacy mute state.
		/// </summary>
		public void UpdateVolumePoints()
		{
			VolumePointHelper[] helpers = m_PointsSection.Execute(() => m_Points.Values.ToArray());
			foreach (VolumePointHelper helper in helpers)
				UpdateVolumePoint(helper);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Updates the volume point to match the privacy mute state.
		/// </summary>
		/// <param name="volumePoint"></param>
		private void UpdateVolumePoint([NotNull] IVolumePoint volumePoint)
		{
			if (volumePoint == null)
				throw new ArgumentNullException("volumePoint");

			VolumePointHelper helper = null;
			if (!m_PointsSection.Execute(() => m_Points.TryGetValue(volumePoint, out helper)))
				throw new InvalidOperationException("Volume point is not registered");

			UpdateVolumePoint(helper);
		}

		/// <summary>
		/// Updates the volume point to match the privacy mute state.
		/// </summary>
		/// <param name="helper"></param>
		private void UpdateVolumePoint([NotNull] VolumePointHelper helper)
		{
			if (helper == null)
				throw new ArgumentNullException("helper");

			IVolumePoint volumePoint = helper.VolumePoint;
			if (volumePoint == null)
				throw new InvalidOperationException("Helper has null volume point");
                       
			switch (volumePoint.MuteType)
			{
				case eMuteType.None:
				case eMuteType.RoomAudio:
					return;

				// Always mute DSP privacy to match conference manager
				case eMuteType.DspPrivacyMute:
					helper.SetIsMuted(m_ConferenceManager.PrivacyMuted);
					break;

				// Only mute microphones as a last resort
				case eMuteType.MicPrivacyMute:
					bool micMute = m_ConferenceManager.PrivacyMuted && PrivacyMuteMics();
					helper.SetIsMuted(micMute);
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// Returns true if microphones should be privacy muted.
		/// Due to AEC we only want to mute mics if we are unable to mute the active
		/// conference endpoint or mute input audio at the DSP level.
		/// </summary>
		/// <returns></returns>
		private bool PrivacyMuteMics()
		{
			// Easy - Are there any DSP mute volume points?
			if (GetVolumePoints().Any(p => p.MuteType == eMuteType.DspPrivacyMute))
				return false;

			// Hard - Do the active conference endpoints support privacy mute?
			IEnumerable<IConferenceDeviceControl> endpoints = m_ConferenceManager.Dialers.GetDialingProviders();
			endpoints = endpoints.Concat(m_ConferenceManager.Dialers.GetFeedbackDialingProviders());

			bool supportsPrivacyMute = endpoints.Where(e => e.GetActiveConference() != null)
			                                    .AnyAndAll(e =>
			                                               e.SupportedConferenceFeatures.HasFlag(eConferenceFeatures.PrivacyMute));
			if (supportsPrivacyMute)
				return false;

			return true;
		}

		#endregion

		#region Conference Manager Callbacks

		/// <summary>
		/// Subscribe to the conference manager events.
		/// </summary>
		/// <param name="conferenceManager"></param>
		private void Subscribe(IConferenceManager conferenceManager)
		{
			conferenceManager.OnPrivacyMuteStatusChange += ConferenceManagerOnPrivacyMuteStatusChange;
		}

		private void ConferenceManagerOnPrivacyMuteStatusChange(object sender, BoolEventArgs boolEventArgs)
		{
			UpdateVolumePoints();
		}

		#endregion

		#region Volume Point Helper Callbacks

		/// <summary>
		/// Subscribe to the volume point helper events.
		/// </summary>
		/// <param name="helper"></param>
		private void Subscribe(VolumePointHelper helper)
		{
			helper.OnVolumeControlIsMutedChanged += HelperOnVolumeControlIsMutedChanged;
		}

		/// <summary>
		/// Unsubscribe from the volume point helper events.
		/// </summary>
		/// <param name="helper"></param>
		private void Unsubscribe(VolumePointHelper helper)
		{
			helper.OnVolumeControlIsMutedChanged -= HelperOnVolumeControlIsMutedChanged;
		}

		/// <summary>
		/// Called when the mute state of the underlying volume control changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="boolEventArgs"></param>
		private void HelperOnVolumeControlIsMutedChanged(object sender, BoolEventArgs boolEventArgs)
		{
			VolumePointHelper helper = sender as VolumePointHelper;
			if (helper == null)
				throw new InvalidOperationException("Unexpected sender");

			UpdateVolumePoint(helper);
		}

		#endregion
	}
}
