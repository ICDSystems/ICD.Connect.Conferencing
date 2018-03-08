﻿using System;
using ICD.Common.Utils.Timers;
using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Conferencing.Cisco.Components.Cameras
{
	public sealed class CiscoCodecCameraDevicePowerControl<T> : AbstractPowerDeviceControl<T>
		where T : CiscoCodecCameraDevice
	{
		private readonly IcdTimer m_Timer;
		private const int CODEC_SLEEP_TIMER_MIN = 120;
		private const long KEEP_AWAKE_TICK_MS = 3600 * 1000;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public CiscoCodecCameraDevicePowerControl(T parent, int id) : base(parent, id)
		{
			m_Timer = new IcdTimer();
			m_Timer.OnElapsed += TimerExpired;
		}

		private void TimerExpired(object sender, EventArgs eventArgs)
		{
			Parent.GetCodec().Wake();
			m_Timer.Restart(KEEP_AWAKE_TICK_MS);
			Parent.GetCodec().ResetSleepTimer(CODEC_SLEEP_TIMER_MIN);
		}

		public override void PowerOn()
		{
			IsPowered = true;
			Parent.GetCodec().Wake();
			m_Timer.Restart(KEEP_AWAKE_TICK_MS);
			Parent.GetCodec().ResetSleepTimer(CODEC_SLEEP_TIMER_MIN);
		}

		public override void PowerOff()
		{
			IsPowered = false;
			m_Timer.Stop();
		}
	}
}