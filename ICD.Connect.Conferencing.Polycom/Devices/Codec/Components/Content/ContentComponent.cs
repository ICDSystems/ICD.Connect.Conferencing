using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Content
{
	public sealed class ContentComponent : AbstractPolycomComponent, IFeedBackComponent
    {
		/// <summary>
		/// Raised when the active content video source changes.
		/// </summary>
		public event EventHandler<ContentVideoSourceEventArgs> OnContentVideoSourceChanged;

		public event EventHandler<PresentationActiveEventArgs> OnPresentationActiveChanged;

		private int? m_ContentVideoSource;
		private bool m_PresentationActive;

		/// <summary>
		/// Gets the content video source (input address) that is currently playing.
		/// </summary>
		public int? ContentVideoSource
		{
			get { return m_ContentVideoSource; }
			private set
			{
				if (value == m_ContentVideoSource)
					return;

				m_ContentVideoSource = value;

				Codec.Logger.Set("Content Video Source", eSeverity.Informational, m_ContentVideoSource);

				OnContentVideoSourceChanged.Raise(this, new ContentVideoSourceEventArgs(m_ContentVideoSource));
			}
		}

		public bool PresentationActive
		{
			get { return m_PresentationActive; }
			private set
			{
				if (value == m_PresentationActive)
					return;

				m_PresentationActive = value;

				Codec.Logger.Set("Presentation Active", eSeverity.Informational, m_PresentationActive);

				OnPresentationActiveChanged.Raise(this, new PresentationActiveEventArgs(m_PresentationActive));
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
        public void InitializeFeedBack()
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
			Codec.Logger.Log(eSeverity.Informational, "Sharing content from video source {0}", videoSource);
			Codec.EnqueueCommand("vcbutton play {0}", videoSource);
		}

		/// <summary>
		/// Stops sending content.
		/// </summary>
		public void Stop()
		{
			Codec.Logger.Log(eSeverity.Informational, "Stopping sharing content");
			Codec.EnqueueCommand("vcbutton stop");
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
					{
						ContentVideoSource = address;
						PresentationActive = true;
					}
					break;

				case "stop":
					ContentVideoSource = null;
					PresentationActive = false;
					break;

				case "farplay":
					ContentVideoSource = null;
					PresentationActive = true;
					break;

				case "farstop":
					PresentationActive = false;
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

			// handling source change
			if (split[1] != "source" || split[2] != "get")
				return;

			string result = split[3];
			if (result == "none")
			{
				ContentVideoSource = null;
				PresentationActive = false;
				return;
			}

			int address;
			if (StringUtils.TryParse(result, out address))
			{
				ContentVideoSource = address;
				PresentationActive = true;
			}
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
			addRow("PresentationActive", PresentationActive);
		}

		#endregion
	}
}
