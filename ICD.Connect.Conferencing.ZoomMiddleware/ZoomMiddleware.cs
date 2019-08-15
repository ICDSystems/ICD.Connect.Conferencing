using ICD.Common.Logging;
using ICD.Common.Logging.Loggers;
using ICD.Common.Utils;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Conferencing.Zoom;
using ICD.Connect.Protocol.Network.Ports.Ssh;

namespace ICD.Connect.Conferencing.ZoomMiddleware
{
	public sealed class ZoomMiddleware
	{
		private readonly ZoomLoopbackServerDevice m_Device;

		public static string IcdSystemsPath { get { return @"C:\ProgramData\ICD Systems"; } }

		public static string ZoomMiddlewarePath { get { return PathUtils.Join(IcdSystemsPath, "ZoomMiddleware"); } }

		public static string LogPath { get { return PathUtils.Join(IcdSystemsPath, "Logs"); } }

		/// <summary>
		/// Constructor.
		/// </summary>
		public ZoomMiddleware(ZoomMiddlewareConfig config)
		{
			// Setup logging
			LoggingCore logger = new LoggingCore{SeverityLevel = eSeverity.Debug};
			logger.AddLogger(new FileLogger(LogPath));
			ServiceProvider.TryAddService<ILoggerService>(logger);

			// Port and defaults
			SshPort port = new SshPort
			{
				Address = "localhost",
				Port = config.ZoomPort,
				Username = config.ZoomUsername,
				Password = config.ZoomPassword
			};

			// Device
			m_Device = new ZoomLoopbackServerDevice
			{
				ListenAddress = config.ListenAddress,
				ListenPort = config.ListenPort
			};

			m_Device.SetPort(port, false);
		}

		public void Start()
		{
			m_Device.Start();
		}

		public void Stop()
		{
			m_Device.Stop();
		}
	}
}
