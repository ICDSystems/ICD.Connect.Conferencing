using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Timers;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Cameras;
using ICD.Connect.Cameras.Controls;
using ICD.Connect.Conferencing.Zoom.Components.Camera;

namespace ICD.Connect.Conferencing.Zoom.Controls
{
	public sealed class ZoomRoomCameraControl : AbstractCameraDeviceControl<ZoomRoom>, IPanTiltControl, IZoomControl
	{
		private readonly CameraComponent m_CameraComponent;

		private Stack<BiDictionary<eCameraControlState, eCameraControlAction>> m_MostRecentCommand;
		private readonly SafeCriticalSection m_UpdateMostRecentCommandSection;

		// Zoom requires PTZ commands to be continued until stopped.
		// So we use timers to continue commands every 500 milliseconds.
		private readonly SafeTimer m_PanTiltContinueTimer;
		private readonly SafeTimer m_ZoomContinueTimer;

		public ZoomRoomCameraControl(ZoomRoom parent, int id)
			: base(parent, id)
		{
			m_MostRecentCommand = new Stack<BiDictionary<eCameraControlState, eCameraControlAction>>();
			m_UpdateMostRecentCommandSection = new SafeCriticalSection();

			m_PanTiltContinueTimer = SafeTimer.Stopped(PanTiltContinueTimerCallback);
			m_ZoomContinueTimer = SafeTimer.Stopped(ZoomContinueTimerCallback);

			m_CameraComponent = parent.Components.GetComponent<CameraComponent>();
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			m_MostRecentCommand = null;

			m_PanTiltContinueTimer.Dispose();
			m_ZoomContinueTimer.Dispose();
		}

		#region Pan Tilt

		/// <summary>
		/// Stops the camera from moving.
		/// </summary>
		void IPanTiltControl.Stop()
		{
			PanTilt(eCameraPanTiltAction.Stop);
		}

		/// <summary>
		/// Begin panning the camera to the left.
		/// </summary>
		public void PanLeft()
		{
			PanTilt(eCameraPanTiltAction.Left);
		}

		/// <summary>
		/// Begin panning the camera to the right.
		/// </summary>
		public void PanRight()
		{
			PanTilt(eCameraPanTiltAction.Right);
		}

		/// <summary>
		/// Begin tilting the camera up.
		/// </summary>
		public void TiltUp()
		{
			PanTilt(eCameraPanTiltAction.Up);
		}

		/// <summary>
		/// Begin tilting the camera down.
		/// </summary>
		public void TiltDown()
		{
			PanTilt(eCameraPanTiltAction.Down);
		}

		/// <summary>
		/// Performs the given pan/tilt action.
		/// </summary>
		/// <param name="action"></param>
		public void PanTilt(eCameraPanTiltAction action)
		{
			// Camera id is always 0 because that is the id for the ZoomRoom's camera.
			switch (action)
			{
				case eCameraPanTiltAction.Left:
					m_CameraComponent.ControlCamera("0", eCameraControlState.Start, eCameraControlAction.Left);
					m_PanTiltContinueTimer.Reset(500);
					UpdateLastCommand(eCameraControlState.Start, eCameraControlAction.Left);
					break;
				case eCameraPanTiltAction.Right:
					m_CameraComponent.ControlCamera("0", eCameraControlState.Start, eCameraControlAction.Right);
					m_PanTiltContinueTimer.Reset(500);
					UpdateLastCommand(eCameraControlState.Start, eCameraControlAction.Right);
					break;
				case eCameraPanTiltAction.Up:
					m_CameraComponent.ControlCamera("0", eCameraControlState.Start, eCameraControlAction.Up);
					m_PanTiltContinueTimer.Reset(500);
					UpdateLastCommand(eCameraControlState.Start, eCameraControlAction.Up);
					break;
				case eCameraPanTiltAction.Down:
					m_CameraComponent.ControlCamera("0", eCameraControlState.Start, eCameraControlAction.Down);
					m_PanTiltContinueTimer.Reset(500);
					UpdateLastCommand(eCameraControlState.Start, eCameraControlAction.Down);
					break;
				case eCameraPanTiltAction.Stop:
					// No command to stop, return.
					if (!m_MostRecentCommand.Any())
					{
						Parent.Log(eSeverity.Warning, "A stop command was issued, but there was no previous command to stop.");
						return;
					}

					m_PanTiltContinueTimer.Stop();
					var previousAction = m_MostRecentCommand.Peek().Values.First();
					m_CameraComponent.ControlCamera("0", eCameraControlState.Stop, previousAction);
					UpdateLastCommand(eCameraControlState.Stop, previousAction);
					break;
				default:
					throw new ArgumentOutOfRangeException("action");
			}
		}

		#endregion

		#region Zoom

		/// <summary>
		/// Stops the camera from moving.
		/// </summary>
		void IZoomControl.Stop()
		{
			Zoom(eCameraZoomAction.Stop);
		}

		/// <summary>
		/// Begin zooming the camera in.
		/// </summary>
		public void ZoomIn()
		{
			Zoom(eCameraZoomAction.ZoomIn);
		}

		/// <summary>
		/// Begin zooming the camera out.
		/// </summary>
		public void ZoomOut()
		{
			Zoom(eCameraZoomAction.ZoomOut);
		}

		/// <summary>
		/// Performs the given zoom action.
		/// </summary>
		public void Zoom(eCameraZoomAction action)
		{
			// Camera id is always 0 because that is the id for the ZoomRoom's camera.
			switch (action)
			{
				case eCameraZoomAction.ZoomIn:
					m_CameraComponent.ControlCamera("0", eCameraControlState.Start, eCameraControlAction.In);
					m_ZoomContinueTimer.Reset(500);
					UpdateLastCommand(eCameraControlState.Start, eCameraControlAction.In);
					break;
				case eCameraZoomAction.ZoomOut:
					m_CameraComponent.ControlCamera("0", eCameraControlState.Start, eCameraControlAction.Out);
					m_ZoomContinueTimer.Reset(500);
					UpdateLastCommand(eCameraControlState.Start, eCameraControlAction.Out);
					break;
				case eCameraZoomAction.Stop:
					// No command to stop, return.
					if (!m_MostRecentCommand.Any())
					{
						Parent.Log(eSeverity.Warning, "A stop command was issued, but there was no previous command to stop.");
						return;
					}

					m_ZoomContinueTimer.Stop();
					var previousAction = m_MostRecentCommand.Peek().Values.First();
					m_CameraComponent.ControlCamera("0", eCameraControlState.Stop, previousAction);
					UpdateLastCommand(eCameraControlState.Stop, previousAction);
					break;
				default:
					throw new ArgumentOutOfRangeException("action");
			}
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Updates the most recent command by storing the state and action of the last issued command.
		/// </summary>
		/// <param name="state"></param>
		/// <param name="action"></param>
		private void UpdateLastCommand(eCameraControlState state, eCameraControlAction action)
		{
			m_UpdateMostRecentCommandSection.Enter();

			try
			{
				// If there is a previous control command remove from the stack.
				if (m_MostRecentCommand.Any())
					m_MostRecentCommand.Pop();

				// Push the most recent command.
				m_MostRecentCommand.Push(new BiDictionary<eCameraControlState, eCameraControlAction>
				{
					{state, action}
				});
			}
			finally
			{
				m_UpdateMostRecentCommandSection.Leave();
			}
		}

		/// <summary>
		/// After a pan tilt command is issued, we continue the command until a stop command is issued.
		/// </summary>
		private void PanTiltContinueTimerCallback()
		{
			var continueAction = m_MostRecentCommand.Peek().Values.First();

			m_CameraComponent.ControlCamera("0", eCameraControlState.Continue, continueAction);
			UpdateLastCommand(eCameraControlState.Continue, continueAction);
			m_PanTiltContinueTimer.Reset(500);
		}

		/// <summary>
		/// After a zoom command is issued, we continue the command until a stop command is issued.
		/// </summary>
		private void ZoomContinueTimerCallback(){
			var continueAction = m_MostRecentCommand.Peek().Values.First();

			m_CameraComponent.ControlCamera("0", eCameraControlState.Continue, continueAction);
			UpdateLastCommand(eCameraControlState.Continue, continueAction);
			m_ZoomContinueTimer.Reset(500);
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			foreach (IConsoleNodeBase node in PanTiltControlConsole.GetConsoleNodes(this))
				yield return node;

			foreach (IConsoleNodeBase node in ZoomControlConsole.GetConsoleNodes(this))
				yield return node;
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
		{
			return base.GetConsoleNodes();
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			PanTiltControlConsole.BuildConsoleStatus(this, addRow);
			ZoomControlConsole.BuildConsoleStatus(this, addRow);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			foreach (IConsoleCommand command in PanTiltControlConsole.GetConsoleCommands(this))
				yield return command;

			foreach (IConsoleCommand command in ZoomControlConsole.GetConsoleCommands(this))
				yield return command;
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
