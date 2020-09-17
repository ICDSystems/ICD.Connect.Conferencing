using System;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Timers;
using ICD.Connect.Conferencing.Cisco.Devices.Codec;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.System;
using ICD.Connect.Devices.Controls.Power;

namespace ICD.Connect.Conferencing.Cisco.Devices.Camera
{
	public sealed class CiscoCodecCameraDevicePowerControl : AbstractPowerDeviceControl<CiscoCodecCameraDevice>
	{
		private const int CODEC_SLEEP_TIMER_MIN = 120;
		private const long KEEP_AWAKE_TICK_MS = 60 * 60 * 1000;

		private readonly SafeTimer m_Timer;

		private SystemComponent m_SystemComponent;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public CiscoCodecCameraDevicePowerControl(CiscoCodecCameraDevice parent, int id)
			: base(parent, id)
		{
			m_Timer = SafeTimer.Stopped(KeepAwakeTimerExpired);

			SetCodec(parent.Codec);
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			m_SystemComponent = null;
			m_Timer.Stop();
			m_Timer.Dispose();
		}

		#region Methods

		/// <summary>
		/// Powers on the device.
		/// </summary>
		protected override void PowerOnFinal()
		{
			if (PowerState == ePowerState.PowerOn)
				return;

			PowerState = ePowerState.PowerOn;

			m_Timer.Reset(0, KEEP_AWAKE_TICK_MS);
		}

		/// <summary>
		/// Powers off the device.
		/// </summary>
		protected override void PowerOffFinal()
		{
			PowerState = ePowerState.PowerOff;

			m_Timer.Stop();
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Called periodically to keep the codec awake.
		/// </summary>
		private void KeepAwakeTimerExpired()
		{
			if (m_SystemComponent == null)
				return;

			if (!m_SystemComponent.Awake)
				m_SystemComponent.Wake();

			m_SystemComponent.ResetSleepTimer(CODEC_SLEEP_TIMER_MIN);
		}

		#endregion

		#region Parent Callbacks

		/// <summary>
		/// Subscribe to the camera events.
		/// </summary>
		/// <param name="parent"></param>
		protected override void Subscribe(CiscoCodecCameraDevice parent)
		{
			base.Subscribe(parent);

			parent.OnCodecChanged += ParentOnCodecChanged;
		}

		/// <summary>
		/// Unsubscribe from the camera events.
		/// </summary>
		/// <param name="parent"></param>
		protected override void Unsubscribe(CiscoCodecCameraDevice parent)
		{
			base.Unsubscribe(parent);

			parent.OnCodecChanged -= ParentOnCodecChanged;
		}

		/// <summary>
		/// Called when the wrapped codec changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void ParentOnCodecChanged(object sender, EventArgs eventArgs)
		{
			SetCodec(Parent.Codec);
		}

		#endregion

		#region Codec Callbacks

		private void SetCodec(CiscoCodecDevice codec)
		{
			Unsubscribe(m_SystemComponent);

			m_SystemComponent = codec == null ? null : codec.Components.GetComponent<SystemComponent>();

			Subscribe(m_SystemComponent);
		}

		private void Subscribe(SystemComponent systemComponent)
		{
			if (systemComponent == null)
				return;

			systemComponent.OnAwakeStateChanged += SystemComponentOnAwakeStateChanged;
		}

		private void Unsubscribe(SystemComponent systemComponent)
		{
			if (systemComponent == null)
				return;

			systemComponent.OnAwakeStateChanged -= SystemComponentOnAwakeStateChanged;
		}

		private void SystemComponentOnAwakeStateChanged(object sender, BoolEventArgs eventArgs)
		{
			// Force the codec to wake up if it goes to sleep and we are using this camera
			if (PowerState == ePowerState.PowerOn && !eventArgs.Data)
				KeepAwakeTimerExpired();
		}

		#endregion
	}
}
