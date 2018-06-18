using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.AutoAnswer;
using ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Camera;
using ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Content;
using ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Dial;
using ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Layout;
using ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Mute;
using ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Sleep;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec.Components
{
	public sealed class PolycomComponentFactory : IDisposable
	{
		private readonly IcdHashSet<AbstractPolycomComponent> m_Components;
		private readonly SafeCriticalSection m_ComponentsSection;

		private static readonly Dictionary<Type, Func<PolycomGroupSeriesDevice, AbstractPolycomComponent>> s_Factories =
			new Dictionary<Type, Func<PolycomGroupSeriesDevice, AbstractPolycomComponent>>
			{
				{typeof(AutoAnswerComponent), codec => new AutoAnswerComponent(codec)},
				{typeof(CameraComponent), codec => new CameraComponent(codec)},
				{typeof(ContentComponent), codec => new ContentComponent(codec)},
				{typeof(DialComponent), codec => new DialComponent(codec)},
				{typeof(LayoutComponent), codec => new LayoutComponent(codec)},
				{typeof(MuteComponent), codec => new MuteComponent(codec)},
				{typeof(SleepComponent), codec => new SleepComponent(codec)},
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
		/// Deconstructor.
		/// </summary>
		~PolycomComponentFactory()
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

		#endregion

		/// <summary>
		/// Release resources.
		/// </summary>
		private void Dispose(bool disposing)
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
	}
}
