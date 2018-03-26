using System;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Timers;
using ICD.Connect.Conferencing.Cisco.Components.System;
using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Conferencing.Cisco.Components.Cameras
{
	public sealed class CiscoCodecCameraDevicePowerControl : AbstractPowerDeviceControl<CiscoCodecCameraDevice>
	{
		private const int CODEC_SLEEP_TIMER_MIN = 120;
		private const long KEEP_AWAKE_TICK_MS = 3600 * 1000;

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
			Subscribe();
			UpdateSystemComponent();
			m_Timer = SafeTimer.Stopped(TimerExpired);
		}

		private void ParentOnCodecChanged(object sender, EventArgs eventArgs)
		{
			UpdateSystemComponent();
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);
			Unsubscribe();
			m_Timer.Stop();
			m_Timer.Dispose();
		}

		private void SystemComponentOnAwakeStateChanged(object sender, BoolEventArgs boolEventArgs)
		{
			if (IsPowered)
				TimerExpired();
		}

		private void TimerExpired()
		{
			if (m_SystemComponent == null)
				return;
			if (!m_SystemComponent.Awake)
				m_SystemComponent.Wake();
			m_SystemComponent.ResetSleepTimer(CODEC_SLEEP_TIMER_MIN);
		}

		public override void PowerOn()
		{
			IsPowered = true;
			m_Timer.Reset(0, KEEP_AWAKE_TICK_MS);
		}

		public override void PowerOff()
		{
			IsPowered = false;
			m_Timer.Stop();
		}

		private void Subscribe()
		{
			Parent.CodecChanged += ParentOnCodecChanged;
		}

		private void Unsubscribe()
		{
			Parent.CodecChanged -= ParentOnCodecChanged;
		}

		private void UpdateSystemComponent()
		{
			CiscoCodec codec = Parent.GetCodec();
			if (codec == null)
				return;

			m_SystemComponent = codec.Components.GetComponent<SystemComponent>();
			m_SystemComponent.OnAwakeStateChanged += SystemComponentOnAwakeStateChanged;
		}
	}
}