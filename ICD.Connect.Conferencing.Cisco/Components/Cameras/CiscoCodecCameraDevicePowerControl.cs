using System;
using ICD.Common.Utils.Timers;
using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Conferencing.Cisco.Components.Cameras
{
	public sealed class CiscoCodecCameraDevicePowerControl<T> : AbstractPowerDeviceControl<T>
		where T : CiscoCodecCameraDevice
	{
		private readonly SafeTimer m_Timer;
		private const int CODEC_SLEEP_TIMER_MIN = 120;
		private const long KEEP_AWAKE_TICK_MS = 3600 * 1000;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public CiscoCodecCameraDevicePowerControl(T parent, int id) : base(parent, id)
		{
			m_Timer = SafeTimer.Stopped(TimerExpired);
		}

		private void TimerExpired()
		{
			Parent.GetCodec().Wake();
			Parent.GetCodec().ResetSleepTimer(CODEC_SLEEP_TIMER_MIN);
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
	}
}