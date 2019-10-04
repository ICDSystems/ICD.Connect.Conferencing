using System;
using System.Collections.Generic;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Zoom.Responses;

namespace ICD.Connect.Conferencing.Zoom.Components.Layout
{
	public sealed class LayoutComponent : AbstractZoomRoomComponent
	{
		#region Events

		/// <summary>
		/// Raised when the share content or the camera content is changed to the thumbnail.
		/// </summary>
		public event EventHandler<LayoutConfigurationEventArgs> OnShareThumbChanged;

		/// <summary>
		/// Raised when the layout style is changed.
		/// </summary>
		public event EventHandler<LayoutConfigurationEventArgs> OnStyleChanged;

		/// <summary>
		/// Raised when the size of the thumbnail is changed.
		/// </summary>
		public event EventHandler<LayoutConfigurationEventArgs> OnSizeChanged;

		/// <summary>
		/// Raised when the position of the thumbnail is changed.
		/// </summary>
		public event EventHandler<LayoutConfigurationEventArgs> OnPositionChanged;

		#endregion
		
		private bool m_ShareThumb;
		private eZoomLayoutStyle m_LayoutStyle;
		private eZoomLayoutSize m_LayoutSize;
		private eZoomLayoutPosition m_LayoutPosition;

		#region Properties

		/// <summary>
		/// True for share content in thumbnail false for camera content in thumbnail.
		/// </summary>
		public bool ShareThumb
		{
			get { return m_ShareThumb; }
			private set
			{
				if (m_ShareThumb == value)
					return;

				m_ShareThumb = value;
				Parent.Log(eSeverity.Informational, "Setting ShareThumb to: {0}", m_ShareThumb);
				OnShareThumbChanged.Raise(this,
				                          new LayoutConfigurationEventArgs(m_ShareThumb, m_LayoutStyle, m_LayoutSize,
				                                                           m_LayoutPosition));
			}
		}

		/// <summary>
		/// Possible ZoomRoom UI Layout styles: Gallery, Speaker, Strip, Share All.
		/// </summary>
		public eZoomLayoutStyle LayoutStyle
		{
			get { return m_LayoutStyle; }
			private set
			{
				if (value == m_LayoutStyle)
					return;

				m_LayoutStyle = value;

				Parent.Log(eSeverity.Informational, "Layout Style set to {0}", m_LayoutStyle);

				OnStyleChanged.Raise(this,
				                     new LayoutConfigurationEventArgs(m_ShareThumb, m_LayoutStyle, m_LayoutSize,
				                                                      m_LayoutPosition));
			}
		}

		/// <summary>
		/// Different sizes for thumbnail with off hiding the thumbnail.
		/// </summary>
		public eZoomLayoutSize LayoutSize
		{
			get { return m_LayoutSize; }
			private set
			{
				if (value == m_LayoutSize)
					return;

				m_LayoutSize = value;

				Parent.Log(eSeverity.Informational, "Layout Size set to {0}", m_LayoutSize);

				OnSizeChanged.Raise(this,
				                     new LayoutConfigurationEventArgs(m_ShareThumb, m_LayoutStyle, m_LayoutSize,
				                                                      m_LayoutPosition));
			}
		}

		/// <summary>
		/// Different positions on the display for the thumbnail.
		/// Currently UpRight, DownRight, UpLeft, DownLeft are supported.
		/// </summary>
		public eZoomLayoutPosition LayoutPosition
		{
			get { return m_LayoutPosition; }
			private set
			{
				if (value == m_LayoutPosition)
					return;

				m_LayoutPosition = value;

				Parent.Log(eSeverity.Informational, "Layout Position set to {0}", m_LayoutPosition);

				OnPositionChanged.Raise(this,
				                     new LayoutConfigurationEventArgs(m_ShareThumb, m_LayoutStyle, m_LayoutSize,
				                                                      m_LayoutPosition));
			}
		}

		#endregion

		#region Constructor

		public LayoutComponent(ZoomRoom parent)
			: base(parent)
		{
			Subscribe(Parent);
		}

		protected override void DisposeFinal()
		{
			base.DisposeFinal();

			OnShareThumbChanged = null;
			OnStyleChanged = null;
			OnSizeChanged = null;
			OnPositionChanged = null;

			Unsubscribe(Parent);
		}

		#endregion

		#region Methods

		protected override void Initialize()
		{
			base.Initialize();

			UpdateLayout();
		}

		public void SetLayoutShareThumb(bool enabled)
		{
			Parent.SendCommand("zConfiguration Call Layout ShareThumb: {0}", enabled ? "on" : "off");
		}

		public void SetLayoutStyle(eZoomLayoutStyle style)
		{
			Parent.SendCommand("zConfiguration Call Layout Style: {0}", style.ToString());
		}

		public void SetLayoutSize(eZoomLayoutSize size)
		{
			Parent.SendCommand("zConfiguration Call Layout Size: {0}", size.ToString());
		}

		public void SetLayoutPosition(eZoomLayoutPosition position)
		{
			if (position > eZoomLayoutPosition.DownLeft)
				throw new ArgumentOutOfRangeException();

			Parent.SendCommand("zConfiguration Call Layout Position: {0}", position.ToString());
		}

		public void HideSelfView(bool enabled)
		{
			Parent.SendCommand("zConfiguration Video hide_conf_self_video: {0}", enabled ? "on" : "off");
		}

		public void UpdateLayout()
		{
			Parent.SendCommand("zConfiguration Call Layout ShareThumb");
			Parent.SendCommand("zConfiguration Call Layout Style");
			Parent.SendCommand("zConfiguration Call Layout Size");
			Parent.SendCommand("zConfiguration Call Layout Position");
		}

		#endregion

		#region Zoom Room Callbacks

		private void Subscribe(ZoomRoom parent)
		{
			parent.RegisterResponseCallback<ClientCallLayoutResponse>(ClientCallLayoutResponseCallback);
			parent.RegisterResponseCallback<CallConfigurationResponse>(CallConfigurationCallback);
		}

		private void Unsubscribe(ZoomRoom parent)
		{
			parent.UnregisterResponseCallback<ClientCallLayoutResponse>(ClientCallLayoutResponseCallback);
			parent.UnregisterResponseCallback<CallConfigurationResponse>(CallConfigurationCallback);
		}

		private void ClientCallLayoutResponseCallback(ZoomRoom zoomroom, ClientCallLayoutResponse response)
		{
			var data = response.CallLayoutConfiguration.LayoutConfigurationHeader.LayoutConfiguration;

			if (data == null)
				return;

			ShareThumb = data.ShareThumb;
			
			if (data.Style != 0)
				LayoutStyle = data.Style;

			if (data.Size != 0)
				LayoutSize = data.Size;

			if (data.Position != 0)
				LayoutPosition = data.Position;
		}

		private void CallConfigurationCallback(ZoomRoom zoomroom, CallConfigurationResponse response)
		{
			var data = response.CallConfiguration.Layout;

			if (data == null)
				return;

			if (data.Size != 0)
				LayoutSize = data.Size;

			if (data.Position != 0)
				LayoutPosition = data.Position;
		}

		#endregion

		#region Console

		public override string ConsoleHelp { get { return "Zoom Room Layout"; } }

		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("ShareThumb", ShareThumb);
			addRow("Style", LayoutStyle);
			addRow("Size", LayoutSize);
			addRow("Position", LayoutPosition);
		}

		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<bool>("SetShareThumb", "SetShareThumb <true/false>",
			                                             b => SetLayoutShareThumb(b));
			yield return new GenericConsoleCommand<eZoomLayoutStyle>("SetStyle",
			                                                         "SetStyle <Gallery, Speaker, Strip, ShareAll>",
			                                                         e => SetLayoutStyle(e));
			yield return new GenericConsoleCommand<eZoomLayoutSize>("SetSize",
			                                                        "SetSize <Off, Size1, Size2, Size3, Strip>",
			                                                        e => SetLayoutSize(e));
			yield return new GenericConsoleCommand<eZoomLayoutPosition>("SetPosition",
			                                                            "SetPosition <UpRight, DownRight, UpLeft, DownLeft>",
			                                                            e => SetLayoutPosition(e));
		}

		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
