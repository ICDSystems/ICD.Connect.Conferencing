using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Conferencing.Vaddio.Devices.AvBridge.Components.Video
{
	public sealed class VideoComponent : AbstractAvBridgeComponent
	{
		public event EventHandler<BoolEventArgs> OnVideoMuteChanged;

		private bool m_VideoMute;

		public bool VideoMute
		{
			get { return m_VideoMute; }
			private set
			{
				if (value == m_VideoMute)
					return;

				m_VideoMute = value;

				OnVideoMuteChanged.Raise(this, new BoolEventArgs(value));
			}
		}

		#region Constructor

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="avBridge"></param>
		public VideoComponent(VaddioAvBridgeDevice avBridge)
			: base(avBridge)
		{
			Subscribe(avBridge);

			if (avBridge.Initialized)
				Initialize();
		}

		/// <summary>
		/// Called to initialize the component.
		/// </summary>
		protected override void Initialize()
		{
			base.Initialize();

			GetVideoInput();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Gets the current video input.
		/// </summary>
		public void GetVideoInput()
		{
			AvBridge.SendCommand("video input get");
		}

		public void SetVideoInput(eVideoInput input)
		{
			if (input == eVideoInput.None)
			{
				AvBridge.Logger.Log(eSeverity.Warning, "Please select a valid video input");
				return;
			}

			AvBridge.SendCommand("video input {0}", input);
		}

		/// <summary>
		/// Gets the current video mute state.
		/// </summary>
		public void GetVideoMute()
		{
			AvBridge.SendCommand("video mute get");
		}

		/// <summary>
		/// Sets the current video mute state.
		/// </summary>
		/// <param name="mute"></param>
		public void SetVideoMute(bool mute)
		{
			string muteString = mute ? "on" : "off";
			AvBridge.SendCommand("video mute {0}", muteString);
		}

		/// <summary>
		/// Toggles the current video mute state.
		/// </summary>
		public void ToggleVideoMute()
		{
			AvBridge.SendCommand("video mute toggle");
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<bool>("SetVideoMute", "<true | false>", m => SetVideoMute(m));
			yield return new ConsoleCommand("ToggleVideoMute", "Toggles the video mute", () => ToggleVideoMute());
		}

		/// <summary>
		/// Shim to avoid "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("VideoMute", VideoMute);
		}

		#endregion
	}
}
