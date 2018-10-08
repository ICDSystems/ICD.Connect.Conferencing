using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Connect.Conferencing.Zoom.Components.Bookings;
using ICD.Connect.Conferencing.Zoom.Components.Call;
using ICD.Connect.Conferencing.Zoom.Components.Camera;
using ICD.Connect.Conferencing.Zoom.Components.Directory;
using ICD.Connect.Conferencing.Zoom.Components.Presentation;
using ICD.Connect.Conferencing.Zoom.Components.System;

namespace ICD.Connect.Conferencing.Zoom.Components
{
	public sealed class ZoomRoomComponentFactory : IDisposable
	{
		private readonly IcdHashSet<AbstractZoomRoomComponent> m_Components;
		private readonly SafeCriticalSection m_ComponentsSection;

		private static readonly Dictionary<Type, Func<ZoomRoom, AbstractZoomRoomComponent>> s_Factories =
			new Dictionary<Type, Func<ZoomRoom, AbstractZoomRoomComponent>>
			{
				{typeof(DialingComponent), zoomRoom => new DialingComponent(zoomRoom)},
				{typeof(DirectoryComponent), zoomRoom => new DirectoryComponent(zoomRoom)},
				{typeof(PresentationComponent), zoomRoom => new PresentationComponent(zoomRoom)},
				{typeof(SystemComponent), zoomRoom => new SystemComponent(zoomRoom)},
				{typeof(CameraComponent), zoomRoom => new CameraComponent(zoomRoom)},
				{typeof(BookingsComponent), zoomRoom => new BookingsComponent(zoomRoom)},
				{typeof(CallComponent), zoomRoom => new CallComponent(zoomRoom)}
			};

		private readonly ZoomRoom m_ZoomRoom;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="zoomRoom"></param>
		public ZoomRoomComponentFactory(ZoomRoom zoomRoom)
		{
			m_Components = new IcdHashSet<AbstractZoomRoomComponent>();
			m_ComponentsSection = new SafeCriticalSection();

			m_ZoomRoom = zoomRoom;
		}

		/// <summary>
		/// Deconstructor.
		/// </summary>
		~ZoomRoomComponentFactory()
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
			where T : AbstractZoomRoomComponent
		{
			m_ComponentsSection.Enter();

			try
			{
				T output = m_Components.OfType<T>().FirstOrDefault() ?? s_Factories[typeof(T)](m_ZoomRoom) as T;
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
		public IEnumerable<AbstractZoomRoomComponent> GetComponents()
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
				foreach (AbstractZoomRoomComponent component in m_Components)
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