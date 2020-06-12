using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Conferencing.Vaddio.Devices.AvBridge.Components.Video
{
	public sealed class VaddioAvBridgeVideoComponent : AbstractVaddioAvBridgeComponent
	{
		public event EventHandler<GenericEventArgs<eVideoInput>> OnVideoInputChanged;

		public event EventHandler<BoolEventArgs> OnVideoMuteChanged;

		private eVideoInput m_VideoInput;
		private bool m_VideoMute;

		public eVideoInput VideoInput
		{
			get { return m_VideoInput; }
			private set
			{
				if (value == m_VideoInput)
					return;

				m_VideoInput = value;

				AvBridge.Logger.Log(eSeverity.Informational, "Video input set to {0}", m_VideoInput);
				OnVideoInputChanged.Raise(this, new GenericEventArgs<eVideoInput>(value));
			}
		}

		public bool VideoMute
		{
			get { return m_VideoMute; }
			private set
			{
				if (value == m_VideoMute)
					return;

				m_VideoMute = value;

				AvBridge.Logger.Log(eSeverity.Informational, "Video mute set to {0}", m_VideoMute);
				OnVideoMuteChanged.Raise(this, new BoolEventArgs(value));
			}
		}

		#region Constructor

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="avBridge"></param>
		public VaddioAvBridgeVideoComponent(VaddioAvBridgeDevice avBridge)
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
			GetVideoMute();
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
				AvBridge.Logger.Log(eSeverity.Warning, "Please select a valid video input - {0}", input);
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

		#region Feedback Handlers

		protected override void Subscribe(VaddioAvBridgeDevice avBridge)
		{
			base.Subscribe(avBridge);

			avBridge.RegisterFeedback("video input", HandleVideoInputFeedback);
			avBridge.RegisterFeedback("video mute", HandleVideoMuteFeedback);
		}

		private void HandleVideoInputFeedback(VaddioAvBridgeSerialResponse response)
		{
			if (response.StatusCode != "OK")
			{
				AvBridge.Logger.Log(eSeverity.Warning, "error in AV Bridge response - {0}", response.StatusCode);
				return;
			}

			switch (response.CommandSetValue)
			{
				case "get":
					VideoInput = (eVideoInput)Enum.Parse(typeof(eVideoInput), response.OptionValue, true);
					break;

				case "auto":
					VideoInput = eVideoInput.auto;
					break;

				case "hdmi":
					VideoInput = eVideoInput.hdmi;
					break;

				case "rgbhv":
					VideoInput = eVideoInput.rgbhv;
					break;

				case "sd":
					VideoInput = eVideoInput.sd;
					break;

				case "ypbpr":
					VideoInput = eVideoInput.ypbpr;
					break;
			}
		}

		private void HandleVideoMuteFeedback(VaddioAvBridgeSerialResponse response)
		{
			if (response.StatusCode != "OK")
			{
				AvBridge.Logger.Log(eSeverity.Warning, "error in AV Bridge response - {0}", response.StatusCode);
				return;
			}

			switch (response.CommandSetValue)
			{
				case "get":
					if (response.OptionValue == "on")
						VideoMute = true;
					if (response.OptionValue == "off")
						VideoMute = false;
					break;

				case "on":
					VideoMute = true;
					break;

				case "off":
					VideoMute = false;
					break;

				case "toggle":
					VideoMute = !VideoMute;
					break;
			}
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

			yield return new GenericConsoleCommand<eVideoInput>("SetVideoInput", "<auto|hdmi|rgbhv|sd|ypbpr>",
			                                                    i => SetVideoInput(i));
			yield return new GenericConsoleCommand<bool>("SetVideoMute", "<true|false>", m => SetVideoMute(m));
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

			addRow("VideoInput", VideoInput);
			addRow("VideoMute", VideoMute);
		}

		#endregion
	}
}
