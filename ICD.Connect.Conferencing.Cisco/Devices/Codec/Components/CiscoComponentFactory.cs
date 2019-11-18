using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Audio;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Bookings;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Cameras;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Diagnostics;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Dialing;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Directory;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Peripherals;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Presentation;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.RoomAnalytics;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.System;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Video;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components
{
	/// <summary>
	/// CiscoComponentFactory provides a facility for lazy-loading components.
	/// </summary>
	public sealed class CiscoComponentFactory : IDisposable
	{
		private static readonly Dictionary<Type, Func<CiscoCodecDevice, ICiscoComponent>> s_Factories =
			new Dictionary<Type, Func<CiscoCodecDevice, ICiscoComponent>>
			{
				{typeof(BookingsComponent), codec => new BookingsComponent(codec)},
				{typeof(DiagnosticsComponent), codec => new DiagnosticsComponent(codec)},
				{typeof(DialingComponent), codec => new DialingComponent(codec)},
				{typeof(DirectoryComponent), codec => new DirectoryComponent(codec)},
				{typeof(NearCamerasComponent), codec => new NearCamerasComponent(codec)},
				{typeof(PeripheralsComponent), codec => new PeripheralsComponent(codec)},
				{typeof(PresentationComponent), codec => new PresentationComponent(codec)},
				{typeof(RoomAnalyticsComponent), codec => new RoomAnalyticsComponent(codec)},
				{typeof(SystemComponent), codec => new SystemComponent(codec)},
				{typeof(VideoComponent), codec => new VideoComponent(codec)},
				{typeof(AudioComponent), codec => new AudioComponent(codec)}
			};

		private readonly Dictionary<Type, ICiscoComponent> m_Components;
		private readonly SafeCriticalSection m_ComponentsSection;

		private readonly CiscoCodecDevice m_Codec;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="codec"></param>
		public CiscoComponentFactory(CiscoCodecDevice codec)
		{
			m_Components = new Dictionary<Type, ICiscoComponent>();
			m_ComponentsSection = new SafeCriticalSection();

			m_Codec = codec;

			// Load components
			foreach (Type type in s_Factories.Keys)
				GetComponent(type);
		}

		/// <summary>
		/// Deconstructor.
		/// </summary>
		~CiscoComponentFactory()
		{
			Dispose(false);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		/// <param name="disposing"></param>
		private void Dispose(bool disposing)
		{
			m_ComponentsSection.Enter();

			try
			{
				foreach (ICiscoComponent component in m_Components.Values)
					component.Dispose();
				m_Components.Clear();
			}
			finally
			{
				m_ComponentsSection.Leave();
			}
		}

		#region Methods

		/// <summary>
		/// Gets the component with the given type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T GetComponent<T>()
			where T : ICiscoComponent
		{
			return (T)GetComponent(typeof(T));
		}

		/// <summary>
		/// Gets the component with the given type.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public ICiscoComponent GetComponent(Type type)
		{
			m_ComponentsSection.Enter();

			try
			{
				ICiscoComponent component;
				if (!m_Components.TryGetValue(type, out component))
				{
					component = s_Factories[type](m_Codec);
					m_Components.Add(type, component);
				}

				return component;
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
		public IEnumerable<ICiscoComponent> GetComponents()
		{
			return m_ComponentsSection.Execute(() => m_Components.Values.OrderBy(c => c.GetType().Name).ToArray());
		}

		#endregion
	}
}
