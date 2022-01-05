#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
#if SIMPLSHARP
using Crestron.SimplSharp.CrestronIO;
#else
using System.IO;
#endif
using ICD.Common.Utils;
using ICD.Common.Utils.Timers;
using ICD.Connect.API.Commands;
using ICD.Connect.Conferencing.Contacts;
using ICD.Connect.Conferencing.DirectoryMiddlewareClient.Model;
using ICD.Connect.Conferencing.DirectoryMiddlewareClient.Requests;
using ICD.Connect.Conferencing.DirectoryMiddlewareClient.Responses;
using System;
using System.Collections.Generic;
using System.Text;
using ICD.Common.Properties;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Devices;
using ICD.Connect.Devices.EventArguments;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Network.Ports.Web;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Settings;

namespace ICD.Connect.Conferencing.DirectoryMiddlewareClient
{
	public sealed class DirectoryMiddlewareClientDevice : AbstractDevice<DirectoryMiddlewareClientDeviceSettings>
	{
		private const string PORT_ACCEPT = "application/json";

		private static readonly JsonSerializer s_Serializer =
#if SIMPLSHARP
			new JsonSerializer();
#else
			JsonSerializer.CreateDefault();
#endif

		private readonly UriProperties m_UriProperties;
		private readonly WebProxyProperties m_WebProxyProperties;

		private IWebPort m_Port;

		public IWebPort Port { get { return m_Port; } }

#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		public DirectoryMiddlewareClientDevice()
		{
			m_UriProperties = new UriProperties();
			m_WebProxyProperties = new WebProxyProperties();
		}

#endregion

#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			SetPort(null);

			base.DisposeFinal(disposing);
		}

		/// <summary>
		/// Performs the search.
		/// </summary>
		/// <param name="query"></param>
		/// <param name="searchFilter"></param>
		/// <param name="page"></param>
		/// <param name="pageSize"></param>
		/// <param name="onResult"></param>
		public void Search(string query, eSearchFilter searchFilter, int page, int pageSize,
			Action<SearchResponse> onResult)
		{
			SearchRequest request = new SearchRequest
			{
				Query = query,
				Filter = searchFilter,
				Page = page,
				PageSize = pageSize
			};

			Get("api/directory/search", request, onResult);
		}

		/// <summary>
		/// Performs the search.
		/// </summary>
		/// <param name="query"></param>
		/// <param name="searchFilter"></param>
		/// <param name="page"></param>
		/// <param name="pageSize"></param>
		/// <param name="onResult"></param>
		public void SearchNames(string query, eSearchFilter searchFilter, int page, int pageSize,
			Action<SearchNamesResponse> onResult)
		{
			SearchRequest request = new SearchRequest
			{
				Query = query,
				Filter = searchFilter,
				Page = page,
				PageSize = pageSize
			};

			Get("api/directory/search-names", request, onResult);
		}

		/// <summary>
		/// Performs the search.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="onResult"></param>
		public void SearchContact(Guid id, Action<Contact> onResult)
		{
			Get("api/directory/" + id, null, onResult);
		}

		/// <summary>
		/// Sets the port for communication with the device.
		/// </summary>
		/// <param name="port"></param>
		[PublicAPI]
		public void SetPort(IWebPort port)
		{
			if (port == m_Port)
				return;

			ConfigurePort(port);

			Unsubscribe(m_Port);
			m_Port = port;
			Subscribe(m_Port);

			if (m_Port != null)
				m_Port.Accept = PORT_ACCEPT;

			UpdateCachedOnlineStatus();
		}

#endregion

#region Private Methods

		/// <summary>
		/// Configures the given port for communication with the device.
		/// </summary>
		/// <param name="port"></param>
		private void ConfigurePort(IWebPort port)
		{
			// URI
			if (port != null)
			{
				port.ApplyDeviceConfiguration(m_UriProperties);
				port.ApplyDeviceConfiguration(m_WebProxyProperties);
			}
		}

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			return m_Port != null;
		}

		private void Get<T>(string relativeOrAbsoluteUri, object body, Action<T> onResult)
		{
			string json = JsonConvert.SerializeObject(body);

			Dictionary<string, List<string>> headers = new Dictionary<string, List<string>>
			{
				{"Content-Type", new List<string> {"application/json"}}
			};

			WebPortResponse webResponse =
				IcdStopwatch.Profile(() =>
					m_Port.Get(relativeOrAbsoluteUri, headers, Encoding.UTF8.GetBytes(json)), "GET");
			if (!webResponse.GotResponse)
				throw new InvalidOperationException("Request failed");

			if (!webResponse.IsSuccessCode)
				throw new InvalidOperationException(string.Format("Request failed - {0}", webResponse.StatusCode));

			T response = Deserialize<T>(webResponse);

			IcdConsole.PrintLine(eConsoleColor.Magenta, webResponse.DataAsString);

			onResult(response);
		}

		private static T Deserialize<T>(WebPortResponse webResponse)
		{
			return IcdStopwatch.Profile(() =>
			{
				using (MemoryStream s = new MemoryStream(webResponse.Data))
				{
					using (StreamReader sr = new StreamReader(s))
					{
						using (JsonReader reader = new JsonTextReader(sr))
						{
							return s_Serializer.Deserialize<T>(reader);
						}
					}
				}
			}, "Deserialize");
		}

#endregion

#region Port Callbacks

		/// <summary>
		/// Subscribe to the port events.
		/// </summary>
		/// <param name="port"></param>
		private void Subscribe(IWebPort port)
		{
			if (port == null)
				return;

			port.OnIsOnlineStateChanged += PortOnIsOnlineStateChanged;
		}

		/// <summary>
		/// Unsubscribe from the port events.
		/// </summary>
		/// <param name="port"></param>
		private void Unsubscribe(IWebPort port)
		{
			if (port == null)
				return;

			port.OnIsOnlineStateChanged -= PortOnIsOnlineStateChanged;
		}

		/// <summary>
		/// Called when the port online state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void PortOnIsOnlineStateChanged(object sender, DeviceBaseOnlineStateApiEventArgs args)
		{
			UpdateCachedOnlineStatus();
		}

#endregion

#region Settings

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(DirectoryMiddlewareClientDeviceSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Port = m_Port == null ? (int?)null : m_Port.Id;

			settings.Copy(m_UriProperties);
			settings.Copy(m_WebProxyProperties);
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			SetPort(null);

			m_UriProperties.ClearUriProperties();
			m_WebProxyProperties.ClearProxyProperties();
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(DirectoryMiddlewareClientDeviceSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			m_UriProperties.Copy(settings);
			m_WebProxyProperties.Copy(settings);

			IWebPort port = null;

			if (settings.Port != null)
			{
				try
				{
					port = factory.GetPortById((int)settings.Port) as IWebPort;
				}
				catch (KeyNotFoundException)
				{
					Logger.Log(eSeverity.Error, "No Web Port with id {0}", settings.Port);
				}	
			}
			SetPort(port);
		}

#endregion

#region Console

		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			string filterHelp = StringUtils.ArrayFormat(EnumUtils.GetValues<eSearchFilter>());

			yield return
				new GenericConsoleCommand<string, eSearchFilter>("Search", string.Format("Search <QUERY> <{0}>", filterHelp),
					(query, filter) => ConsoleSearch(query, filter));
			yield return
				new GenericConsoleCommand<string, eSearchFilter>("SearchNames", string.Format("SearchNames <QUERY> <{0}>", filterHelp),
					(query, filter) => ConsoleSearchNames(query, filter));
			yield return
				new GenericConsoleCommand<Guid>("SearchContact", "SearchContact <GUID>", id => ConsoleSearchContact(id));
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		private void ConsoleSearch(string query, eSearchFilter searchFilter)
		{
			Search(query, searchFilter, 0, 10, r => { });
		}

		private void ConsoleSearchNames(string query, eSearchFilter searchFilter)
		{
			SearchNames(query, searchFilter, 0, 10, r => { });
		}

		private void ConsoleSearchContact(Guid id)
		{
			SearchContact(id, r => { });
		}

#endregion
	}
}
