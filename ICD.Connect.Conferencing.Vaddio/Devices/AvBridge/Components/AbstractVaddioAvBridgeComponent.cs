using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Conferencing.Vaddio.Devices.AvBridge.Components
{
	public abstract class AbstractVaddioAvBridgeComponent : IVaddioAvBridgeComponent
	{
		#region Properties

		public VaddioAvBridgeDevice AvBridge { get; private set; }

		/// <summary>
		/// Gets the name of the node.
		/// </summary>
		public string ConsoleName { get { return GetType().GetNameWithoutGenericArity(); } }

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public string ConsoleHelp { get { return string.Empty; } }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="avBridge"></param>
		protected AbstractVaddioAvBridgeComponent(VaddioAvBridgeDevice avBridge)
		{
			AvBridge = avBridge;
		}

		/// <summary>
		/// Deconstructor.
		/// </summary>
		~AbstractVaddioAvBridgeComponent()
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
		protected virtual void Dispose(bool disposing)
		{
			Unsubscribe(AvBridge);
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
		/// Gets the child console nodes.
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

		protected virtual void Subscribe(VaddioAvBridgeDevice avBridge)
		{
			if (avBridge == null)
				return;

			avBridge.OnInitializedChanged += AvBridgeOnInitializedChanged;
			avBridge.OnConnectedStateChanged += AvBridgeOnConnectedStateChanged;
		}

		protected virtual void Unsubscribe(VaddioAvBridgeDevice avBridge)
		{
			if (avBridge == null)
				return;

			avBridge.OnInitializedChanged -= AvBridgeOnInitializedChanged;
			avBridge.OnConnectedStateChanged -= AvBridgeOnConnectedStateChanged;
		}

		/// <summary>
		/// Called to initialize the component.
		/// </summary>
		protected virtual void Initialize()
		{
		}

		/// <summary>
		/// Called when the component connects/disconnects to the codec.
		/// </summary>
		protected virtual void ConnectionStatusChanged(bool state)
		{
		}

		#endregion

		#region Private Methods

		private void AvBridgeOnInitializedChanged(object sender, BoolEventArgs args)
		{
			if (args.Data)
				Initialize();
		}

		private void AvBridgeOnConnectedStateChanged(object sender, BoolEventArgs args)
		{
			ConnectionStatusChanged(args.Data);
		}

		#endregion
	}
}
