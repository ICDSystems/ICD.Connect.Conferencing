using System;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Timers;
using ICD.Connect.Conferencing.Polycom.Devices.Codec;
using ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Sleep;
using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Conferencing.Polycom.Devices.Camera
{
	public sealed class PolycomGroupSeriesCameraDevicePowerControl : AbstractPowerDeviceControl<PolycomGroupSeriesCameraDevice>
	{
		private const long KEEP_AWAKE_TICK_MS = 60 * 60 * 1000;

		private readonly SafeTimer m_Timer;

		private SleepComponent m_SleepComponent;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public PolycomGroupSeriesCameraDevicePowerControl(PolycomGroupSeriesCameraDevice parent, int id)
			: base(parent, id)
		{
			Subscribe();

			UpdateSleepComponent();

			m_Timer = SafeTimer.Stopped(TimerExpired);
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			Unsubscribe();

			m_SleepComponent = null;
			m_Timer.Stop();
			m_Timer.Dispose();
		}

		private void TimerExpired()
		{
			if (m_SleepComponent == null)
				return;

			if (!m_SleepComponent.Awake)
				m_SleepComponent.Wake();
		}

		#region Methods

		/// <summary>
		/// Powers on the device.
		/// </summary>
		protected override void PowerOnFinal()
		{
			IsPowered = true;

			m_Timer.Reset(0, KEEP_AWAKE_TICK_MS);
		}

		/// <summary>
		/// Powers off the device.
		/// </summary>
		protected override void PowerOffFinal()
		{
			IsPowered = false;

			m_Timer.Stop();
		}

		#endregion

		#region Parent Callbacks

		/// <summary>
		/// Subscribe to the parent events.
		/// </summary>
		private void Subscribe()
		{
			Parent.OnCodecChanged += ParentOnCodecChanged;
		}

		/// <summary>
		/// Subscribe to the parent events.
		/// </summary>
		private void Unsubscribe()
		{
			Parent.OnCodecChanged -= ParentOnCodecChanged;
		}

		/// <summary>
		/// Called when the parent codec changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void ParentOnCodecChanged(object sender, EventArgs eventArgs)
		{
			UpdateSleepComponent();
		}

		#endregion

		#region Sleep Component Callbacks

		private void UpdateSleepComponent()
		{
			if (m_SleepComponent != null)
				m_SleepComponent.OnAwakeStateChanged -= SleepComponentOnAwakeStateChanged;

			m_SleepComponent = null;

			PolycomGroupSeriesDevice codec = Parent.GetCodec();
			if (codec == null)
				return;

			m_SleepComponent = codec.Components.GetComponent<SleepComponent>();
			m_SleepComponent.OnAwakeStateChanged += SleepComponentOnAwakeStateChanged;
		}

		/// <summary>
		/// Called when the parent codec awake state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="boolEventArgs"></param>
		private void SleepComponentOnAwakeStateChanged(object sender, BoolEventArgs boolEventArgs)
		{
			if (IsPowered)
				TimerExpired();
		}

		#endregion
	}
}
