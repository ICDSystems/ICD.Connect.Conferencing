using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;

namespace ICD.Connect.Conferencing.Polycom.Devices.Components
{
	public sealed class PolycomComponentFactory
	{
		private readonly IcdHashSet<AbstractPolycomComponent> m_Components;
		private readonly SafeCriticalSection m_ComponentsSection;

		private static readonly Dictionary<Type, Func<PolycomGroupSeriesDevice, AbstractPolycomComponent>> s_Factories =
			new Dictionary<Type, Func<PolycomGroupSeriesDevice, AbstractPolycomComponent>>
			{
			};

		private readonly PolycomGroupSeriesDevice m_Codec;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="codec"></param>
		public PolycomComponentFactory(PolycomGroupSeriesDevice codec)
		{
			m_Components = new IcdHashSet<AbstractPolycomComponent>();
			m_ComponentsSection = new SafeCriticalSection();

			m_Codec = codec;
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			m_ComponentsSection.Enter();

			try
			{
				foreach (AbstractPolycomComponent component in m_Components)
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
			where T : AbstractPolycomComponent
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
		public IEnumerable<AbstractPolycomComponent> GetComponents()
		{
			return m_ComponentsSection.Execute(() => m_Components.ToArray());
		}
	}
}