using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
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
	public sealed class CiscoComponentFactory : IDisposable
	{
		private readonly Dictionary<Type, AbstractCiscoComponent> m_Components;
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

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="codec"></param>
		public CiscoComponentFactory(CiscoCodecDevice codec)
		{
			m_Components = new Dictionary<Type, AbstractCiscoComponent>();
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
			Type key = typeof(T);

			m_ComponentsSection.Enter();

			try
			{
				AbstractCiscoComponent component;
				if (!m_Components.TryGetValue(key, out component))
				{
					component = s_Factories[key](m_Codec) as T;
					m_Components.Add(key, component);
				}

				return component as T;
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
			return m_ComponentsSection.Execute(() => m_Components.Values.ToArray());
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
				foreach (AbstractCiscoComponent component in m_Components.Values)
					component.Dispose();
				m_Components.Clear();
			}
			finally
			{
				m_ComponentsSection.Leave();
			}
		}
	}
}
