using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Timers;
using ICD.Connect.Cameras;
using ICD.Connect.Conferencing.Zoom.Components.Camera;
using ICD.Connect.Conferencing.Zoom.Responses;

namespace ICD.Connect.Conferencing.Zoom.Controls.Camera
{
	public sealed class ZoomRoomCameraRepeater : IDisposable
	{
		private readonly CameraComponent m_CameraComponent;

		/// <summary>
		/// Mapping of eCameraPanTiltAction to Zoom zCommand text
		/// </summary>
		private static readonly BiDictionary<eCameraPanTiltAction, eCameraControlAction> s_PanTiltActionToZoom =
			new BiDictionary
				<eCameraPanTiltAction, eCameraControlAction>
				{
					{eCameraPanTiltAction.Up, eCameraControlAction.Up},
					{eCameraPanTiltAction.Down, eCameraControlAction.Down},
					{eCameraPanTiltAction.Left, eCameraControlAction.Left},
					{eCameraPanTiltAction.Right, eCameraControlAction.Right},
				};

		/// <summary>
		/// Mapping of eCameraZoomAction to Zoom zCommand text
		/// </summary>
		private static readonly BiDictionary<eCameraZoomAction, eCameraControlAction> s_ZoomActionToZoom =
			new BiDictionary
				<eCameraZoomAction, eCameraControlAction>
				{
					{eCameraZoomAction.ZoomIn, eCameraControlAction.In},
					{eCameraZoomAction.ZoomOut, eCameraControlAction.Out}
				};

		private Stack<KeyValuePair<eCameraControlState, eCameraControlAction>> m_MostRecentCommand;
		private readonly SafeCriticalSection m_UpdateMostRecentCommandSection;

		// Zoom requires PTZ commands to be continued until stopped.
		// So we use timers to continue commands every 500 milliseconds.
		private readonly SafeTimer m_PanTiltContinueTimer;
		private readonly SafeTimer m_ZoomContinueTimer;

		private bool m_HaveControl;
		private readonly string m_UserId;

		public bool HaveControl { get { return m_HaveControl; } }
		public string UserId { get { return m_UserId; } }

		public ZoomRoomCameraRepeater(CameraComponent cameraComponent, string userId)
		{
			m_UserId = userId;
			m_HaveControl = m_UserId == "0";

			m_MostRecentCommand = new Stack<KeyValuePair<eCameraControlState, eCameraControlAction>>();
			m_UpdateMostRecentCommandSection = new SafeCriticalSection();

			m_PanTiltContinueTimer = SafeTimer.Stopped(PanTiltContinueTimerCallback);
			m_ZoomContinueTimer = SafeTimer.Stopped(ZoomContinueTimerCallback);

			m_CameraComponent = cameraComponent;
			Subscribe(m_CameraComponent);
			m_HaveControl = m_UserId == "0";
		}

		public void Dispose()
		{
			m_MostRecentCommand = null;

			m_PanTiltContinueTimer.Dispose();
			m_ZoomContinueTimer.Dispose();

			Unsubscribe(m_CameraComponent);
		}

		#region Control Methods

		public void PanTilt(eCameraPanTiltAction action)
		{
			if (action == eCameraPanTiltAction.Stop)
			{
				StopPanTilt();
				return;
			}

			if (!m_HaveControl)
			{
				RequestControl();
				return;
			}

			m_CameraComponent.ControlCamera(m_UserId, eCameraControlState.Start,
			                                s_PanTiltActionToZoom.GetValue(action));
			m_PanTiltContinueTimer.Reset(500);
			UpdateLastCommand(eCameraControlState.Start, s_PanTiltActionToZoom.GetValue(action));
		}

		public void StopPanTilt()
		{
			if (!m_HaveControl)
			{
				RequestControl();
				return;
			}

			// If there is no previous command to stop, return.
			if (!m_MostRecentCommand.Any())
				return;

			m_PanTiltContinueTimer.Stop();

			var previousAction = m_MostRecentCommand.Peek().Value;
			m_CameraComponent.ControlCamera(m_UserId, eCameraControlState.Stop, previousAction);
			UpdateLastCommand(eCameraControlState.Stop, previousAction);
		}

		public void Zoom(eCameraZoomAction action)
		{
			if (action == eCameraZoomAction.Stop)
			{
				StopZoom();
				return;
			}

			if (!m_HaveControl)
			{
				RequestControl();
				return;
			}

			m_CameraComponent.ControlCamera(m_UserId, eCameraControlState.Start, s_ZoomActionToZoom.GetValue(action));
			m_ZoomContinueTimer.Reset(500);
			UpdateLastCommand(eCameraControlState.Start, s_ZoomActionToZoom.GetValue(action));
		}

		public void StopZoom()
		{
			if (!m_HaveControl)
			{
				RequestControl();
				return;
			}

			// If there is no previous command to stop, return.
			if (!m_MostRecentCommand.Any())
				return;

			m_ZoomContinueTimer.Stop();

			var previousAction = m_MostRecentCommand.Peek().Value;
			m_CameraComponent.ControlCamera(m_UserId, eCameraControlState.Stop, previousAction);
			UpdateLastCommand(eCameraControlState.Stop, previousAction);
		}

		public void RequestControl()
		{
			// When requesting/giving remote access the control action input does not matter so we just use left.
			m_CameraComponent.ControlCamera(m_UserId, eCameraControlState.RequestRemote, eCameraControlAction.Left);
		}

		public void GiveUpControl()
		{
			// When requesting/giving remote access the control action input does not matter so we just use left.
			m_CameraComponent.ControlCamera(m_UserId, eCameraControlState.GiveUpRemote, eCameraControlAction.Left);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// After a pan tilt command is issued, we continue the command until a stop command is issued.
		/// </summary>
		private void PanTiltContinueTimerCallback()
		{
			var continueAction = m_MostRecentCommand.Peek().Value;

			m_CameraComponent.ControlCamera("0", eCameraControlState.Continue, continueAction);
			UpdateLastCommand(eCameraControlState.Continue, continueAction);
			m_PanTiltContinueTimer.Reset(500);
		}

		/// <summary>
		/// After a zoom command is issued, we continue the command until a stop command is issued.
		/// </summary>
		private void ZoomContinueTimerCallback()
		{
			var continueAction = m_MostRecentCommand.Peek().Value;

			m_CameraComponent.ControlCamera("0", eCameraControlState.Continue, continueAction);
			UpdateLastCommand(eCameraControlState.Continue, continueAction);
			m_ZoomContinueTimer.Reset(500);
		}

		private void UpdateLastCommand(eCameraControlState state, eCameraControlAction action)
		{
			m_UpdateMostRecentCommandSection.Enter();

			try
			{
				// If there is a previous control command remove from the stack.
				if (m_MostRecentCommand.Any())
					m_MostRecentCommand.Pop();

				// Push the most recent command.
				m_MostRecentCommand.Push(new KeyValuePair<eCameraControlState, eCameraControlAction>(state, action));
			}
			finally
			{
				m_UpdateMostRecentCommandSection.Leave();
			}
		}

		#endregion

		#region Camera Component Callbacks

		private void Subscribe(CameraComponent cameraComponent)
		{
			cameraComponent.OnCameraControlNotification += CameraComponentOnCameraControlNotification;
			cameraComponent.OnZoomRoomGaveUpFarEndControl += CameraComponentOnZoomRoomGaveUpFarEndControl;
		}

		private void Unsubscribe(CameraComponent cameraComponent)
		{
			cameraComponent.OnCameraControlNotification -= CameraComponentOnCameraControlNotification;
			cameraComponent.OnZoomRoomGaveUpFarEndControl += CameraComponentOnZoomRoomGaveUpFarEndControl;
		}

		private void CameraComponentOnCameraControlNotification(object sender, CameraControlNotificationEventArgs e)
		{
			if (e.Data.UserId == m_UserId)
				m_HaveControl =
					e.Data.State == eCameraControlNotificationState.ZRCCameraControlStateControlRequestToRemote;
		}

		private void CameraComponentOnZoomRoomGaveUpFarEndControl(object sender, StringEventArgs e)
		{
			// If we gave up far end control of the participant's camera we don't have far end control.
			if (e.Data == m_UserId)
				m_HaveControl = false;
		}

		#endregion
	}
}
