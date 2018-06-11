using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Conferencing.Polycom.Devices.Controls
{
	public sealed class PolycomCodecPowerControl : AbstractPowerDeviceControl<PolycomGroupSeriesDevice>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public PolycomCodecPowerControl(PolycomGroupSeriesDevice parent, int id)
			: base(parent, id)
		{
		}

		#region Methods

		/// <summary>
		/// Powers on the device.
		/// </summary>
		public override void PowerOn()
		{
			throw new System.NotImplementedException();
		}

		/// <summary>
		/// Powers off the device.
		/// </summary>
		public override void PowerOff()
		{
			throw new System.NotImplementedException();
		}

		#endregion
	}
}
