using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Cameras.Controls;
using ICD.Connect.Cameras.Devices;

namespace ICD.Connect.Conferencing.ConferenceManagers
{
	public sealed class ConferenceManagerCameras
	{
		/// <summary>
		/// Raised when a camera is registered/deregistered.
		/// </summary>
		public event EventHandler OnCamerasChanged;

		/// <summary>
		/// Raised when the active camera for the room changes.
		/// </summary>
		public event EventHandler<GenericEventArgs<ICameraDevice>> OnActiveCameraChanged;

		private ICameraDevice m_ActiveCamera;

		private readonly IConferenceManager m_ConferenceManager;
		private readonly IcdHashSet<ICameraDevice> m_Cameras; 
		private readonly SafeCriticalSection m_CamerasSection;

		#region Properties

		/// <summary>
		/// Gets the camera that is currently active for conferencing.
		/// </summary>
		[CanBeNull]
		public ICameraDevice ActiveCamera
		{
			get { return m_ActiveCamera; }
			private set
			{
				if (value == m_ActiveCamera)
					return;

				m_ActiveCamera = value;

				OnActiveCameraChanged.Raise(this, m_ActiveCamera);
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="conferenceManager"></param>
		public ConferenceManagerCameras([NotNull] IConferenceManager conferenceManager)
		{
			if (conferenceManager == null)
				throw new ArgumentNullException("conferenceManager");

			m_ConferenceManager = conferenceManager;
			m_Cameras = new IcdHashSet<ICameraDevice>();
			m_CamerasSection = new SafeCriticalSection();

			Subscribe(m_ConferenceManager);
		}

		#region Methods

		/// <summary>
		/// Sets the active camera for conferencing.
		/// </summary>
		/// <param name="activeCamera"></param>
		public void SetActiveCamera([CanBeNull] ICameraDevice activeCamera)
		{
			ActiveCamera = activeCamera;
		}

		/// <summary>
		/// Deregisters all of the registered cameras.
		/// </summary>
		public void Clear()
		{
			foreach (ICameraDevice point in GetCameras())
				DeregisterCamera(point);
		}

		/// <summary>
		/// Gets the registered cameras.
		/// </summary>
		/// <returns></returns>
		[NotNull]
		public IEnumerable<ICameraDevice> GetCameras()
		{
			return m_CamerasSection.Execute(() => m_Cameras.ToArray());
		}

		/// <summary>
		/// Registers the camera.
		/// </summary>
		/// <param name="camera"></param>
		public bool RegisterCamera([NotNull] ICameraDevice camera)
		{
			if (camera == null)
				throw new ArgumentNullException("camera");

			m_CamerasSection.Enter();

			try
			{
				if (m_Cameras.Add(camera))
					return false;

				Subscribe(camera);
			}
			finally
			{
				m_CamerasSection.Leave();
			}

			UpdateCamera(camera);

			OnCamerasChanged.Raise(this);
			return true;
		}

		/// <summary>
		/// Deregisters the camera.
		/// </summary>
		/// <param name="camera"></param>
		public bool DeregisterCamera([NotNull] ICameraDevice camera)
		{
			if (camera == null)
				throw new ArgumentNullException("camera");

			m_CamerasSection.Enter();

			try
			{
				if (!m_Cameras.Remove(camera))
					return false;

				Unsubscribe(camera);
			}
			finally
			{
				m_CamerasSection.Leave();
			}

			OnCamerasChanged.Raise(this);
			return true;
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Updates the cameras to match the camera privacy mute state.
		/// </summary>
		private void UpdateCameras()
		{
			foreach (ICameraDevice camera in GetCameras())
				UpdateCamera(camera);
		}

		/// <summary>
		/// Updates the camera to match the camera privacy mute state.
		/// </summary>
		/// <param name="camera"></param>
		private void UpdateCamera([NotNull] ICameraDevice camera)
		{
			if (camera == null)
				throw new ArgumentNullException("camera");

			if (camera.SupportedCameraFeatures.HasFlag(eCameraFeatures.Mute))
				camera.MuteCamera(m_ConferenceManager.CameraPrivacyMuted);
		}

		#endregion

		#region Conference Manager Callbacks

		/// <summary>
		/// Subscribe to the conference manager events.
		/// </summary>
		/// <param name="conferenceManager"></param>
		private void Subscribe(IConferenceManager conferenceManager)
		{
			conferenceManager.OnCameraPrivacyMuteStatusChange += ConferenceManagerOnCameraPrivacyMuteStatusChange;
		}

		/// <summary>
		/// Called when the conference manager enables/disables camera privacy mute.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="boolEventArgs"></param>
		private void ConferenceManagerOnCameraPrivacyMuteStatusChange(object sender, BoolEventArgs boolEventArgs)
		{
			UpdateCameras();
		}

		#endregion

		#region Camera Callbacks

		/// <summary>
		/// Subscribe to the camera events.
		/// </summary>
		/// <param name="camera"></param>
		private void Subscribe(ICameraDevice camera)
		{
			camera.OnCameraMuteStateChanged += CameraOnCameraMuteStateChanged;
		}

		/// <summary>
		/// Unsubscribe from the camera events.
		/// </summary>
		/// <param name="camera"></param>
		private void Unsubscribe(ICameraDevice camera)
		{
			camera.OnCameraMuteStateChanged -= CameraOnCameraMuteStateChanged;
		}

		/// <summary>
		/// Called when a camera enters/exits privacy mute.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="boolEventArgs"></param>
		private void CameraOnCameraMuteStateChanged(object sender, BoolEventArgs boolEventArgs)
		{
			ICameraDevice camera = sender as ICameraDevice;
			if (camera == null)
				throw new InvalidOperationException("Unexpected sender");

			UpdateCamera(camera);
		}

		#endregion
	}
}
