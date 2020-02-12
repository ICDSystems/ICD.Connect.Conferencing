using System.Collections.Generic;
using ICD.Common.Logging;
using ICD.Common.Logging.Loggers;
using ICD.Common.Utils;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Zoom;
using ICD.Connect.Protocol.Network.Devices.ConsoleServer;
using ICD.Connect.Protocol.Network.Ports.Ssh;

namespace ICD.Connect.Conferencing.ZoomMiddleware
{
	public sealed class ZoomMiddleware : IConsoleNode
	{
		private readonly ZoomLoopbackServerDevice m_Device;
		private readonly ConsoleServerDevice m_Console;
		private readonly SshPort m_Port;

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
			m_Port = new SshPort
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
			m_Device.SetPort(m_Port, false);

			// Console
			m_Console = new ConsoleServerDevice
			{
				Port = (ushort)(config.ListenPort + 1)
			};
			ApiConsole.RegisterChild(this);
		}

		public void Start()
		{
			m_Device.Start();
		}

		public void Stop()
		{
			m_Device.Stop();
		}

		#region Console

		/// <summary>
		/// Gets the name of the node.
		/// </summary>
		public string ConsoleName { get { return GetType().Name; } }

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public string ConsoleHelp { get { return "Prints a table with options that help navigate to the server device, SshPort and Status."; } }

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			yield return m_Device;
			yield return m_Port;
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield break;
		}

		#endregion
	}
}
