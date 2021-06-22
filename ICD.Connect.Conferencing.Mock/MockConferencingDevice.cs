using System;
using ICD.Connect.Conferencing.Devices;
using ICD.Connect.Devices;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Devices.Mock;
using ICD.Connect.Settings;

namespace ICD.Connect.Conferencing.Mock
{
	public sealed class MockConferencingDevice : AbstractMockDevice<MockConferencingDeviceSettings>, IMockConferencingDevice
	{
		#region Private Memebers

		private readonly CodecInputTypes m_InputTypes;

		#endregion

		/// <summary>
		/// Configured information about how the input connectors should be used.
		/// </summary>
		public CodecInputTypes InputTypes { get { return m_InputTypes; } }

		/// <summary>
		/// The default camera used by the conference device.
		/// </summary>
		public IDeviceBase DefaultCamera { get; set; }

		/// <summary>
		/// Constructor.
		/// </summary>
		public MockConferencingDevice()
		{
			m_InputTypes = new CodecInputTypes();
			m_InputTypes.SetInputType(1, eCodecInputType.Camera);
			m_InputTypes.SetInputType(2, eCodecInputType.Content);
			m_InputTypes.SetInputType(3, eCodecInputType.None);
		}

		/// <summary>
		/// Override to add controls to the device.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		/// <param name="addControl"></param>
		protected override void AddControls(MockConferencingDeviceSettings settings, IDeviceFactory factory, Action<IDeviceControl> addControl)
		{
			base.AddControls(settings, factory, addControl);

			addControl(new MockVideoConferenceRouteControl(this, 0));
			addControl(new MockTraditionalConferenceDeviceControl(this, 1));
			addControl(new MockDirectoryControl(this, 2));
		}



		public void SetInputTypeForInput(int address, eCodecInputType type)
		{
			m_InputTypes.SetInputType(address, type);
		}
	}
}
