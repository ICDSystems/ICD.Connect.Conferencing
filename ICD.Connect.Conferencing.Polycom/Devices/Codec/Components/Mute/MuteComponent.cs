using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Mute
{
	public sealed class MuteComponent : AbstractPolycomComponent, IFeedBackComponent
    {
		private const string MUTE_REGEX = @"mute (?'near'near|far) (?'on'on|off)";
		private const string VIDEO_MUTE_REGEX = @"videomute near (?'on'on|off)";

		/// <summary>
		/// Raised when the near privacy mute state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnMutedNearChanged;

		/// <summary>
		/// Raised when the far mute state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnMutedFarChanged;

		/// <summary>
		/// Raised when the video mute state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnVideoMutedChanged;

		private bool m_MutedNear;
		private bool m_MutedFar;
		private bool m_VideoMuted;

		#region Properties

		/// <summary>
		/// Gets the near privacy mute state.
		/// </summary>
		public bool MutedNear
		{
			get { return m_MutedNear; }
			private set
			{
				if (value == m_MutedNear)
					return;

				m_MutedNear = value;

				Codec.Logger.Set("Muted Near", eSeverity.Informational, m_MutedNear);

				OnMutedNearChanged.Raise(this, new BoolEventArgs(m_MutedNear));
			}
		}

		/// <summary>
		/// Gets the far mute state.
		/// </summary>
		public bool MutedFar
		{
			get { return m_MutedFar; }
			private set
			{
				if (value == m_MutedFar)
					return;

				m_MutedFar = value;

				Codec.Logger.Set("Muted Far", eSeverity.Informational, m_MutedFar);

				OnMutedFarChanged.Raise(this, new BoolEventArgs(m_MutedFar));
			}
		}

		/// <summary>
		/// Gets the video muted state.
		/// </summary>
		public bool VideoMuted
		{
			get { return m_VideoMuted; }
			private set
			{
				if (value == m_VideoMuted)
					return;

				m_VideoMuted = value;

				Codec.Logger.Set("Video Muted", eSeverity.Informational, m_VideoMuted);

				OnVideoMutedChanged.Raise(this, new BoolEventArgs(m_VideoMuted));
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="codec"></param>
		public MuteComponent(PolycomGroupSeriesDevice codec)
			: base(codec)
		{
			Subscribe(Codec);

			Codec.RegisterFeedback("mute", HandleMute);
			Codec.RegisterFeedback("videomute", HandleVideoMute);

			if (Codec.Initialized)
				Initialize();
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			OnMutedNearChanged = null;
			OnMutedFarChanged = null;
			OnVideoMutedChanged = null;

			base.Dispose(disposing);
		}

		/// <summary>
		/// Called to initialize the component.
		/// </summary>
		protected override void Initialize()
		{
			base.Initialize();

			InitializeFeedBack();

			Codec.EnqueueCommand("mute near get");
			Codec.EnqueueCommand("mute far get");
			Codec.EnqueueCommand("videomute near get");
		}

		/// <summary>
		/// Called to initialize the feedbacks.
		/// </summary>
		public void InitializeFeedBack()
		{
			Codec.EnqueueCommand("mute register");
			Codec.EnqueueCommand("notify mutestatus");
		}

		/// <summary>
		/// Handles mute messages from the device.
		/// </summary>
		/// <param name="data"></param>
		private void HandleMute(string data)
		{
			// mute near on
			// mute far off

			Match match = Regex.Match(data, MUTE_REGEX);
			if (!match.Success)
				return;

			bool muted = match.Groups["on"].Value == "on";
			bool near = match.Groups["near"].Value == "near";

			if (near)
				MutedNear = muted;
			else
				MutedFar = muted;
		}

		/// <summary>
		/// Handles video mute messages from the device.
		/// </summary>
		/// <param name="data"></param>
		private void HandleVideoMute(string data)
		{
			// videomute near on
			// videomute near off

			Match match = Regex.Match(data, VIDEO_MUTE_REGEX);
			if (!match.Success)
				return;

			VideoMuted = match.Groups["on"].Value == "on";
		}

		#region Methods

		/// <summary>
		/// Enables/disables near end privacy mute.
		/// </summary>
		/// <param name="mute"></param>
		public void MuteNear(bool mute)
		{
			Codec.EnqueueCommand("mute near {0}", mute ? "on" : "off");
			Codec.Logger.Log(eSeverity.Informational, "Setting near mute {0}", mute ? "on" : "off");
		}

		/// <summary>
		/// Enables/disables muting transmission of local video to far site. 
		/// </summary>
		/// <param name="mute"></param>
		public void MuteVideo(bool mute)
		{
			Codec.EnqueueCommand("videomute near {0}", mute ? "on" : "off");
			Codec.Logger.Log(eSeverity.Informational, "Setting near video mute {0}", mute ? "on" : "off");
		}

		/// <summary>
		/// Toggles near end privacy mute.
		/// </summary>
		public void ToggleMuteNear()
		{
			Codec.EnqueueCommand("mute near toggle");
			Codec.Logger.Log(eSeverity.Informational, "Toggling near mute");
		}

		/// <summary>
		/// Toggles far end mute.
		/// </summary>
		public void ToggleMuteFar()
		{
			Codec.EnqueueCommand("mute far toggle");
			Codec.Logger.Log(eSeverity.Informational, "Toggling far mute");
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

			yield return new GenericConsoleCommand<bool>("MuteNear", "MuteNear <true/false>", m => MuteNear(m));
			yield return new GenericConsoleCommand<bool>("MuteVideo", "MuteVideo <true/false>", m => MuteVideo(m));
			yield return new ConsoleCommand("MuteNearToggle", "", () => ToggleMuteNear());
			yield return new ConsoleCommand("MuteFarToggle", "", () => ToggleMuteFar());
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

			addRow("MutedNear", MutedNear);
			addRow("MutedFar", MutedFar);
		}

		#endregion
	}
}
