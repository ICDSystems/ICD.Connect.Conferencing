using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Content
{
	public sealed class ContentComponent : AbstractPolycomComponent
	{
		/// <summary>
		/// Raised when the active content video source changes.
		/// </summary>
		public event EventHandler<ContentVideoSourceEventArgs> OnContentVideoSourceChanged;

		private int? m_ContentVideoSource;

		/// <summary>
		/// Gets the content video source that is currently playing.
		/// </summary>
		public int? ContentVideoSource
		{
			get { return m_ContentVideoSource; }
			private set
			{
				if (value == m_ContentVideoSource)
					return;

				m_ContentVideoSource = value;

				Codec.Log(eSeverity.Informational, "ContentVideoSource set to {0}", m_ContentVideoSource);

				OnContentVideoSourceChanged.Raise(this, new ContentVideoSourceEventArgs(m_ContentVideoSource));
			}
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="codec"></param>
		public ContentComponent(PolycomGroupSeriesDevice codec)
			: base(codec)
		{
			Subscribe(Codec);

			Codec.RegisterFeedback("Control", HandleControl);
			Codec.RegisterFeedback("vcbutton", HandleVcButton);

			if (Codec.Initialized)
				Initialize();
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			OnContentVideoSourceChanged = null;

			base.Dispose(disposing);
		}

		/// <summary>
		/// Called to initialize the component.
		/// </summary>
		protected override void Initialize()
		{
			base.Initialize();

			InitializeFeedBack();

			Codec.EnqueueCommand("vcbutton get");
			Codec.EnqueueCommand("vcbutton source get");
		}


		/// <summary>
		/// Called to initialize the feedbacks.
		/// </summary>
		protected void InitializeFeedBack()
		{
			Codec.EnqueueCommand("vcbutton register");
		}

		#region Methods

		/// <summary>
		/// Starts sending content from the specified video source.
		/// </summary>
		/// <param name="videoSource"></param>
		public void Play(int videoSource)
		{
			Codec.EnqueueCommand("vcbutton play {0}", videoSource);
			Codec.Log(eSeverity.Informational, "Sharing content from video source {0}", videoSource);
		}

		/// <summary>
		/// Stops sending content.
		/// </summary>
		public void Stop()
		{
			Codec.EnqueueCommand("vcbutton stop");
			Codec.Log(eSeverity.Informational, "Stopping sharing content");
		}

		#endregion

		/// <summary>
		/// Handles control messages from the device.
		/// </summary>
		/// <param name="data"></param>
		private void HandleControl(string data)
		{
			// Control event: vcbutton source 4
			// Control event: vcbutton play
			// Control event: vcbutton stop
			// Control event: vcbutton farplay

			string[] split = data.Split();
			if (split.Length < 4)
				return;

			if (split[1] != "event:" || split[2] != "vcbutton")
				return;

			switch (split[3])
			{
				case "source":
					int address;
					if (StringUtils.TryParse(split[4], out address))
						ContentVideoSource = address;
					break;

				case "stop":
					ContentVideoSource = null;
					break;
			}
		}

		/// <summary>
		/// Handles vcbutton messages from the device.
		/// </summary>
		/// <param name="data"></param>
		private void HandleVcButton(string data)
		{
			// vcbutton source get 1
			// vcbutton source get none
			// vcbutton source get succeeded
			// vcbutton play 7
			// vcbutton play succeeded
			// vcbutton play failed

			string[] split = data.Split();
			if (split.Length != 4)
				return;

			if (split[1] != "source" || split[2] != "get")
				return;

			string result = split[3];
			if (result == "none")
			{
				ContentVideoSource = null;
				return;
			}

			int address;
			if (StringUtils.TryParse(result, out address))
				ContentVideoSource = address;
		}

		#region Console

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<int>("Play", "Play <ADDRESS>", i => Play(i));
			yield return new ConsoleCommand("Stop", "", () => Stop());
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

			addRow("ContentVideoSource", ContentVideoSource);
		}

		#endregion
	}
}
