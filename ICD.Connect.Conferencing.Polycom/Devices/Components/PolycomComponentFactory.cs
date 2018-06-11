using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Polycom.Devices.Components.AutoAnswer;
using ICD.Connect.Conferencing.Polycom.Devices.Components.Layout;
using ICD.Connect.Conferencing.Polycom.Devices.Components.Mute;
using ICD.Connect.Conferencing.Polycom.Devices.Components.Sleep;

namespace ICD.Connect.Conferencing.Polycom.Devices.Components
{
	public sealed class PolycomComponentFactory : IDisposable, IConsoleNode
	{
		private readonly IcdHashSet<AbstractPolycomComponent> m_Components;
		private readonly SafeCriticalSection m_ComponentsSection;

		private static readonly Dictionary<Type, Func<PolycomGroupSeriesDevice, AbstractPolycomComponent>> s_Factories =
			new Dictionary<Type, Func<PolycomGroupSeriesDevice, AbstractPolycomComponent>>
			{
				{typeof(AutoAnswerComponent), codec => new AutoAnswerComponent(codec)},
				{typeof(LayoutComponent), codec => new LayoutComponent(codec)},
				{typeof(MuteComponent), codec => new MuteComponent(codec)},
				{typeof(SleepComponent), codec => new SleepComponent(codec)},
			};

		private readonly PolycomGroupSeriesDevice m_Codec;

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