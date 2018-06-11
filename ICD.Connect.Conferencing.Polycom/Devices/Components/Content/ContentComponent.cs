using System;
using System.Collections.Generic;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Conferencing.Polycom.Devices.Components.Content
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

				Codec.Log(eSeverity.Informational, "MutedFar set to {0}", m_ContentVideoSource);

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

			Codec.SendCommand("vcbutton register");
			Codec.SendCommand("vcbutton get");
		}

		#region Methods

		/// <summary>
		/// Starts sending content from the specified video source.
		/// </summary>
		/// <param name="videoSource"></param>
		public void Play(int videoSource)
		{
			Codec.SendCommand("vcbutton play {0}", videoSource);
			Codec.Log(eSeverity.Informational, "Sharing content from video source {0}", videoSource);
		}

		/// <summary>
		/// Stops sending content.
		/// </summary>
		public void Stop()
		{
			Codec.SendCommand("vcbutton stop");
			Codec.Log(eSeverity.Informational, "Stopping sharing content");
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
