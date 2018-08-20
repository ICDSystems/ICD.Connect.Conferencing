using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Conferencing.Zoom.Components
{
	public abstract class AbstractZoomRoomComponent : IDisposable, IConsoleNode
	{
		#region Properties

		/// <summary>
		/// Gets the ZoomRoom.
		/// </summary>
		public ZoomRoom Parent { get; private set; }

		/// <summary>
		/// Gets the name of the node in the console.
		/// </summary>
		public virtual string ConsoleName { get { return GetType().Name; } }

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public virtual string ConsoleHelp { get { return string.Empty; } }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		protected AbstractZoomRoomComponent(ZoomRoom parent)
		{
			Parent = parent;
			Subscribe(Parent);
		}

		/// <summary>
		/// Deconstructor.
		/// </summary>
		~AbstractZoomRoomComponent()
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
			Unsubscribe(Parent);

			DisposeFinal();
		}

		protected virtual void DisposeFinal()
		{
		}

		#endregion

		#region Methods

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public virtual void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
		}

		/// <summary>
		/// Gets the child console node groups.
		/// </summary>
		/// <returns></returns>
		public virtual IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			yield break;
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public virtual IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield break;
		}

		#endregion

		#region Protected Methods

		/// <summary>
		/// Called to initialize the component.
		/// </summary>
		protected virtual void Initialize()
		{
		}

		/// <summary>
		/// Called when the component connects/disconnects to the ZoomRoom.
		/// </summary>
		protected virtual void ConnectionStatusChanged(bool state)
		{
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Subscribes to the ZoomRoom events.
		/// </summary>
		/// <param name="zoomRoom"></param>
		private void Subscribe(ZoomRoom zoomRoom)
		{
			if (zoomRoom == null)
				return;

			zoomRoom.OnInitializedChanged += ZoomRoomOnInitializedChanged;
			zoomRoom.OnConnectedStateChanged += ZoomRoomOnConnectionStateChanged;
		}

		/// <summary>
		/// Unsubscribes from the ZoomRoom events.
		/// </summary>
		/// <param name="zoomRoom"></param>
		private void Unsubscribe(ZoomRoom zoomRoom)
		{
			if (zoomRoom == null)
				return;

			zoomRoom.OnInitializedChanged -= ZoomRoomOnInitializedChanged;
			zoomRoom.OnConnectedStateChanged -= ZoomRoomOnConnectionStateChanged;
		}

		/// <summary>
		/// Called when the ZoomRoom initializes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void ZoomRoomOnInitializedChanged(object sender, BoolEventArgs args)
		{
			if (args.Data)
				Initialize();
		}

		/// <summary>
		/// Called when the ZoomRoom connects/disconnects
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void ZoomRoomOnConnectionStateChanged(object sender, BoolEventArgs args)
		{
			ConnectionStatusChanged(args.Data);
		}

		#endregion
	}
}