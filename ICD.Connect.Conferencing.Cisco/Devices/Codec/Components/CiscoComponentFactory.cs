using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Cameras;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Diagnostics;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Dialing;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Directory;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Peripherals;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Presentation;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.System;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Video;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components
{
	/// <summary>
	/// CiscoComponentFactory provides a facility for lazy-loading components.
	/// </summary>
	public sealed class CiscoComponentFactory : IDisposable, IConsoleNode
	{
		private readonly IcdHashSet<AbstractCiscoComponent> m_Components;
		private readonly SafeCriticalSection m_ComponentsSection;

		private static readonly Dictionary<Type, Func<CiscoCodecDevice, AbstractCiscoComponent>> s_Factories =
			new Dictionary<Type, Func<CiscoCodecDevice, AbstractCiscoComponent>>
			{
				{typeof(DiagnosticsComponent), codec => new DiagnosticsComponent(codec)},
				{typeof(DialingComponent), codec => new DialingComponent(codec)},
				{typeof(DirectoryComponent), codec => new DirectoryComponent(codec)},
				{typeof(NearCamerasComponent), codec => new NearCamerasComponent(codec)},
				{typeof(PeripheralsComponent), codec => new PeripheralsComponent(codec)},
				{typeof(PresentationComponent), codec => new PresentationComponent(codec)},
				{typeof(SystemComponent), codec => new SystemComponent(codec)},
				{typeof(VideoComponent), codec => new VideoComponent(codec)}
			};

		private readonly CiscoCodecDevice m_Codec;

		#region Properties

		/// <summary>
		/// Gets the name of the node.
		/// </summary>
		public string ConsoleName { get { return "Components"; } }

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public string ConsoleHelp { get { return string.Empty; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="codec"></param>
		public CiscoComponentFactory(CiscoCodecDevice codec)
		{
			m_Components = new IcdHashSet<AbstractCiscoComponent>();
			m_ComponentsSection = new SafeCriticalSection();

			m_Codec = codec;

			// Add some default components
			GetComponent<DiagnosticsComponent>();
			GetComponent<PeripheralsComponent>();
		}

		/// <summary>
		/// Deconstructor.
		/// </summary>
		~CiscoComponentFactory()
		{
			Dispose(false);
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
		}

		/// <summary>
		/// Gets the component with the given type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T GetComponent<T>()
			where T : AbstractCiscoComponent
		{
			m_ComponentsSection.Enter();

			try
			{
				T output = m_Components.OfType<T>().FirstOrDefault() ?? s_Factories[typeof(T)](m_Codec) as T;
				m_Components.Add(output);

				return output;
			}
			finally
			{
				m_ComponentsSection.Leave();
			}
		}

		/// <summary>
		/// Returns the cached components.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<AbstractCiscoComponent> GetComponents()
		{
			return m_ComponentsSection.Execute(() => m_Components.ToArray());
		}

		#endregion

		/// <summary>
		/// Release resources.
		/// </summary>
		/// <param name="disposing"></param>
		private void Dispose(bool disposing)
		{
			m_ComponentsSection.Enter();

			try
			{
				foreach (AbstractCiscoComponent component in m_Components)
					component.Dispose();
				m_Components.Clear();
			}
			finally
			{
				m_ComponentsSection.Leave();
			}
		}

		#region Console

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			return GetComponents().OrderBy(c => c.GetType().Name).Cast<IConsoleNodeBase>();
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
