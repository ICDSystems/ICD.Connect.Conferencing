using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Connect.Conferencing.Cisco.Components.Cameras;
using ICD.Connect.Conferencing.Cisco.Components.Diagnostics;
using ICD.Connect.Conferencing.Cisco.Components.Dialing;
using ICD.Connect.Conferencing.Cisco.Components.Directory;
using ICD.Connect.Conferencing.Cisco.Components.Peripherals;
using ICD.Connect.Conferencing.Cisco.Components.Presentation;
using ICD.Connect.Conferencing.Cisco.Components.System;
using ICD.Connect.Conferencing.Cisco.Components.Video;

namespace ICD.Connect.Conferencing.Cisco.Components
{
	/// <summary>
	/// CiscoComponentFactory provides a facility for lazy-loading components.
	/// </summary>
	public sealed class CiscoComponentFactory : IDisposable
	{
		private readonly IcdHashSet<AbstractCiscoComponent> m_Components;
		private readonly SafeCriticalSection m_ComponentsSection;

		private static readonly Dictionary<Type, Func<CiscoCodec, AbstractCiscoComponent>> s_Factories =
			new Dictionary<Type, Func<CiscoCodec, AbstractCiscoComponent>>
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

		private readonly CiscoCodec m_Codec;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="codec"></param>
		public CiscoComponentFactory(CiscoCodec codec)
		{
			m_Components = new IcdHashSet<AbstractCiscoComponent>();
			m_ComponentsSection = new SafeCriticalSection();

			m_Codec = codec;

			// Add some default components
			GetComponent<DiagnosticsComponent>();
			GetComponent<PeripheralsComponent>();
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
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
	}
}
