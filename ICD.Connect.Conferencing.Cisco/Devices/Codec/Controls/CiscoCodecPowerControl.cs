using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.System;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Devices.Controls.Power;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Controls
{
	public sealed class CiscoCodecPowerControl : AbstractPowerDeviceControl<CiscoCodecDevice>
	{
		private readonly SystemComponent m_System;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public CiscoCodecPowerControl(CiscoCodecDevice parent, int id)
			: base(parent, id)
		{
			m_System = parent.Components.GetComponent<SystemComponent>();

			Subscribe(m_System);

			UpdatePower();
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			Unsubscribe(m_System);
		}

		#region Methods

		/// <summary>
		/// Powers on the device.
		/// </summary>
		protected override void PowerOnFinal()
		{
			m_System.Wake();
		}

		/// <summary>
		/// Powers off the device.
		/// </summary>
		protected override void PowerOffFinal()
		{
			m_System.Standby();
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Updates the power state.
		/// </summary>
		private void UpdatePower()
		{
			PowerState = m_System.Awake ? ePowerState.PowerOn : ePowerState.PowerOff;
		}

		#endregion

		#region System Component Callbacks

		/// <summary>
		/// Subscribe to the system component events.
		/// </summary>
		/// <param name="system"></param>
		private void Subscribe(SystemComponent system)
		{
			system.OnAwakeStateChanged += SystemOnAwakeStateChanged;
		}

		/// <summary>
		/// Unsubscribe from the system component events.
		/// </summary>
		/// <param name="system"></param>
		private void Unsubscribe(SystemComponent system)
		{
			system.OnAwakeStateChanged -= SystemOnAwakeStateChanged;
		}

		/// <summary>
		/// Called when the system component awake status changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="boolEventArgs"></param>
		private void SystemOnAwakeStateChanged(object sender, BoolEventArgs boolEventArgs)
		{
			UpdatePower();
		}

		#endregion
	}
}
