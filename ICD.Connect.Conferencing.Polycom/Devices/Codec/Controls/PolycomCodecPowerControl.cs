using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Sleep;
using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec.Controls
{
	public sealed class PolycomCodecPowerControl : AbstractPowerDeviceControl<PolycomGroupSeriesDevice>
	{
		private readonly SleepComponent m_SleepComponent;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public PolycomCodecPowerControl(PolycomGroupSeriesDevice parent, int id)
			: base(parent, id)
		{
			m_SleepComponent = parent.Components.GetComponent<SleepComponent>();

			Subscribe(m_SleepComponent);
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			Unsubscribe(m_SleepComponent);
		}

		#region Methods

		/// <summary>
		/// Powers on the device.
		/// </summary>
		protected override void PowerOnFinal()
		{
			m_SleepComponent.Wake();
		}

		/// <summary>
		/// Powers off the device.
		/// </summary>
		protected override void PowerOffFinal()
		{
			m_SleepComponent.Sleep();
		}

		#endregion

		#region AutoAnswer Callbacks

		/// <summary>
		/// Subscribe to the component events.
		/// </summary>
		/// <param name="sleepComponent"></param>
		private void Subscribe(SleepComponent sleepComponent)
		{
			sleepComponent.OnAwakeStateChanged += SleepComponentOnAwakeStateChanged;
		}

		/// <summary>
		/// Unsubscribe from the component events.
		/// </summary>
		/// <param name="sleepComponent"></param>
		private void Unsubscribe(SleepComponent sleepComponent)
		{
			sleepComponent.OnAwakeStateChanged -= SleepComponentOnAwakeStateChanged;
		}

		/// <summary>
		/// Called when the awake state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="boolEventArgs"></param>
		private void SleepComponentOnAwakeStateChanged(object sender, BoolEventArgs boolEventArgs)
		{
			IsPowered = m_SleepComponent.Awake;
		}

		#endregion
	}
}
