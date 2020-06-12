using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Connect.Conferencing.Vaddio.Devices.AvBridge.Components.Audio;
using ICD.Connect.Conferencing.Vaddio.Devices.AvBridge.Components.Video;

namespace ICD.Connect.Conferencing.Vaddio.Devices.AvBridge.Components
{
	/// <summary>
	/// VaddioAvBridgeComponentFactory provides a facility for lazy-loading components.
	/// </summary>
	public sealed class VaddioAvBridgeComponentFactory : IDisposable
	{

		private static readonly Dictionary<Type, Func<VaddioAvBridgeDevice, IVaddioAvBridgeComponent>> s_Factories =
			new Dictionary<Type, Func<VaddioAvBridgeDevice, IVaddioAvBridgeComponent>>
			{
				{typeof(VaddioAvBridgeAudioComponent), avBridge => new VaddioAvBridgeAudioComponent(avBridge)},
				{typeof(VaddioAvBridgeVideoComponent), avBridge => new VaddioAvBridgeVideoComponent(avBridge)}
			};

		private readonly Dictionary<Type, IVaddioAvBridgeComponent> m_Components;
		private readonly SafeCriticalSection m_ComponentsSection;

		private readonly VaddioAvBridgeDevice m_AvBridge;

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="avBridge"></param>
		public VaddioAvBridgeComponentFactory(VaddioAvBridgeDevice avBridge)
		{
			m_Components = new Dictionary<Type, IVaddioAvBridgeComponent>();
			m_ComponentsSection = new SafeCriticalSection();

			m_AvBridge = avBridge;

			// Load components
			foreach (Type type in s_Factories.Keys)
				GetComponent(type);
		}

		/// <summary>
		/// Deconstructor.
		/// </summary>
		~VaddioAvBridgeComponentFactory()
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
				foreach (IVaddioAvBridgeComponent component in m_Components.Values)
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
			where T : IVaddioAvBridgeComponent
		{
			return (T)GetComponent(typeof(T));
		}

		public IVaddioAvBridgeComponent GetComponent(Type type)
		{
			m_ComponentsSection.Enter();

			try
			{
				IVaddioAvBridgeComponent component;
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
		public IEnumerable<IVaddioAvBridgeComponent> GetComponents()
		{
			return m_ComponentsSection.Execute(() => m_Components.Values.OrderBy(c => c.GetType().Name).ToArray());
		}

		#endregion
	}
}
