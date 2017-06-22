using System;
using System.Collections.Generic;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Common.EventArguments;

namespace ICD.Connect.Conferencing.Cisco.Components
{
	/// <summary>
	/// AbstractCiscoComponent is a base class for Cisco modules.
	/// </summary>
	public abstract class AbstractCiscoComponent : IDisposable, IConsoleNode
	{
		#region Properties

		/// <summary>
		/// Gets the Codec.
		/// </summary>
		public CiscoCodec Codec { get; private set; }

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
		/// <param name="codec"></param>
		protected AbstractCiscoComponent(CiscoCodec codec)
		{
			Codec = codec;
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		public virtual void Dispose()
		{
			Unsubscribe(Codec);
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
		/// Subscribes to the codec events.
		/// </summary>
		/// <param name="codec"></param>
		protected virtual void Subscribe(CiscoCodec codec)
		{
			if (codec == null)
				return;

			codec.OnInitializedChanged += CodecOnInitializedChanged;
			codec.OnConnectedStateChanged += CodecOnConnectedStateChanged;
		}

		/// <summary>
		/// Unsubscribes from the codec events.
		/// </summary>
		/// <param name="codec"></param>
		protected virtual void Unsubscribe(CiscoCodec codec)
		{
			if (codec == null)
				return;

			codec.OnInitializedChanged -= CodecOnInitializedChanged;
			codec.OnConnectedStateChanged -= CodecOnConnectedStateChanged;
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

		/// <summary>
		/// Called when the codec initializes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void CodecOnInitializedChanged(object sender, BoolEventArgs args)
		{
			if (args.Data)
				Initialize();
		}

		/// <summary>
		/// Called when the codec connects/disconnects
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void CodecOnConnectedStateChanged(object sender, BoolEventArgs args)
		{
			ConnectionStatusChanged(args.Data);
		}

		#endregion
	}
}
