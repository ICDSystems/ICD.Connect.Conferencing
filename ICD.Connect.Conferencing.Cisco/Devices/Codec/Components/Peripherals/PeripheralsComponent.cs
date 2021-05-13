using System;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Timers;
using ICD.Common.Utils.Xml;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Peripherals
{
	/// <summary>
	/// PeripheralsComponent is responsible for the heartbeat to keep the 
	/// program registered with the codec.
	/// </summary>
	public sealed class PeripheralsComponent : AbstractCiscoComponent
	{
		private const long HEARTBEAT_MILLISECONDS = 30 * 1000;

		public event EventHandler<FloatEventArgs> OnAirQualityIndexChanged;

		private readonly SafeTimer m_HeartbeatTimer;

		private float m_AirQualityIndex;

		public float AirQualityIndex
		{
			get { return m_AirQualityIndex; }
			private set
			{
				value = MathUtils.Clamp(value, 0, 5);
				if (Math.Abs(m_AirQualityIndex - value) < 0.01f)
					return;

				m_AirQualityIndex = value;
				OnAirQualityIndexChanged.Raise(this, m_AirQualityIndex);
			}
		}

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="codec"></param>
		public PeripheralsComponent(CiscoCodecDevice codec)
			: base(codec)
		{
			m_HeartbeatTimer = SafeTimer.Stopped(HeartbeatCallback);

			Subscribe(Codec);

			if (Codec.Initialized)
				Initialize();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			m_HeartbeatTimer.Dispose();

			base.Dispose(disposing);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Called to initialize the component.
		/// </summary>
		protected override void Initialize()
		{
			base.Initialize();

			string command = "xCommand Peripherals Connect";
			//command += string.Format(" HardwareInfo: \"{0}\"");
			command += string.Format(" ID: \"{0}\"", Codec.PeripheralsId);
			command += string.Format(" Name: \"{0}\"", GetType());
			command += string.Format(" NetworkAddress: \"{0}\"", IcdEnvironment.NetworkAddresses.FirstOrDefault());
			//command += string.Format(" SerialNumber: \"{0}\"", );
			//command += string.Format(" SoftwareInfo: \"{0}\"", );
			command += " Type: ControlSystem";

			Codec.SendCommand(command);

			m_HeartbeatTimer.Reset(HEARTBEAT_MILLISECONDS, HEARTBEAT_MILLISECONDS);
		}

		/// <summary>
		/// Sends the heartbeat command.
		/// </summary>
		private void HeartbeatCallback()
		{
			//if (Codec.IsConnected)
				Codec.SendCommand("xCommand Peripherals HeartBeat ID: \"{0}\"", Codec.PeripheralsId);
		}

		#endregion

		#region Codec Callbacks

		protected override void Subscribe(CiscoCodecDevice codec)
		{
			base.Subscribe(codec);

			codec.RegisterParserCallback(ParseAirQualityIndex, CiscoCodecDevice.XSTATUS_ELEMENT, "Peripherals", "ConnectedDevice", "RoomAnalytics", "AirQuality", "Index");
		}

		protected override void Unsubscribe(CiscoCodecDevice codec)
		{
			base.Unsubscribe(codec);

			codec.UnregisterParserCallback(ParseAirQualityIndex, CiscoCodecDevice.XSTATUS_ELEMENT, "Peripherals", "ConnectedDevice", "RoomAnalytics", "AirQuality", "Index");
		}

		private void ParseAirQualityIndex(CiscoCodecDevice codec, string resultid, string xml)
		{
			string content = XmlUtils.GetInnerXml(xml);
			AirQualityIndex = float.Parse(content);
		}

		#endregion
	}
}
