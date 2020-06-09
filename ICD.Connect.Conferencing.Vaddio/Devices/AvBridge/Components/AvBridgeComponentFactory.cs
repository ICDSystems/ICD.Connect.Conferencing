using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Connect.Conferencing.Vaddio.Devices.AvBridge.Components.Audio;

namespace ICD.Connect.Conferencing.Vaddio.Devices.AvBridge.Components
{
	/// <summary>
	/// AvBridgeComponentFactory provides a facility for lazy-loading components.
	/// </summary>
	public sealed class AvBridgeComponentFactory : IDisposable
	{

		private static readonly Dictionary<Type, Func<VaddioAvBridgeDevice, IAvBridgeComponent>> s_Factories =
			new Dictionary<Type, Func<VaddioAvBridgeDevice, IAvBridgeComponent>>
			{
				{typeof(AudioComponent), avBridge => new AudioComponent(avBridge)},
				{typeof(VideoComponent), avBridge => new VideoComponent(avBridge)}
			};

		private readonly Dictionary<Type, IAvBridgeComponent> m_Components;
		private readonly SafeCriticalSection m_ComponentsSection;

		private readonly VaddioAvBridgeDevice m_AvBridge;

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="avBridge"></param>
		public AvBridgeComponentFactory(VaddioAvBridgeDevice avBridge)
		{
			m_Components = new Dictionary<Type, IAvBridgeComponent>();
			m_ComponentsSection = new SafeCriticalSection();

			m_AvBridge = avBridge;

			// Load components
			foreach (Type type in s_Factories.Keys)
				GetComponent(type);
		}

		/// <summary>
		/// Deconstructor.
		/// </summary>
		~AvBridgeComponentFactory()
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
				foreach (IAvBridgeComponent component in m_Components.Values)
					component.Dispose();
				m_Components.Clear();
			}
			finally
			{
				m_ComponentsSection.Leave();
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Gets the component with the given type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T GetComponent<T>()
			where T : IAvBridgeComponent
		{
			return (T)GetComponent(typeof(T));
		}

		public IAvBridgeComponent GetComponent(Type type)
		{
			m_ComponentsSection.Enter();

			try
			{
				IAvBridgeComponent component;
				if (!m_Components.TryGetValue(type, out component))
				{
					component = s_Factories[type](m_AvBridge);
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
		public IEnumerable<IAvBridgeComponent> GetComponents()
		{
			return m_ComponentsSection.Execute(() => m_Components.Values.OrderBy(c => c.GetType().Name).ToArray());
		}

		#endregion
	}
}