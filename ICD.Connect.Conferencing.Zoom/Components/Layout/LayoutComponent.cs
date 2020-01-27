using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
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
		public event EventHandler<BoolEventArgs> OnShareThumbChanged;

		/// <summary>
		/// Raised when the layout style is changed.
		/// </summary>
		public event EventHandler<ZoomLayoutStyleEventArgs> OnStyleChanged;

		/// <summary>
		/// Raised when the size of the thumbnail is changed.
		/// </summary>
		public event EventHandler<ZoomLayoutSizeEventArgs> OnSizeChanged;

		/// <summary>
		/// Raised when the position of the thumbnail is changed.
		/// </summary>
		public event EventHandler<ZoomLayoutPositionEventArgs> OnPositionChanged;

		/// <summary>
		/// Raised when selfview becomes enabled or disabled.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnSelfViewEnabledChanged;

		/// <summary>
		/// Raised when the availability of any layout features changes.
		/// </summary>
		public event EventHandler<GenericEventArgs<ZoomLayoutAvailability>> OnCallLayoutAvailabilityChanged;

		#endregion

		private bool m_ShareThumb;
		private eZoomLayoutStyle m_LayoutStyle;
		private eZoomLayoutSize m_LayoutSize;
		private eZoomLayoutPosition m_LayoutPosition;
		private bool m_SelfViewEnabled;
		private ZoomLayoutAvailability m_LayoutAvailability;

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

				Parent.Log(eSeverity.Informational, "ShareThumb set to: {0}", m_ShareThumb);

				OnShareThumbChanged.Raise(this, new BoolEventArgs(m_ShareThumb));
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

				OnStyleChanged.Raise(this, new ZoomLayoutStyleEventArgs(m_LayoutStyle));
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
				
				OnSizeChanged.Raise(this, new ZoomLayoutSizeEventArgs(m_LayoutSize));
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

				OnPositionChanged.Raise(this, new ZoomLayoutPositionEventArgs(m_LayoutPosition));
			}
		}

		/// <summary>
		/// Returns true if selfview is currently enabled.
		/// </summary>
		public bool SelfViewEnabled
		{
			get { return m_SelfViewEnabled; }
			private set
			{
				if (value == m_SelfViewEnabled)
					return;

				m_SelfViewEnabled = value;

				Parent.Log(eSeverity.Informational, "SelfViewEnabled set to {0}", m_SelfViewEnabled);

				OnSelfViewEnabledChanged.Raise(this, new BoolEventArgs(m_SelfViewEnabled));
			}
		}

		public ZoomLayoutAvailability LayoutAvailability
		{
			get { return m_LayoutAvailability; }
			private set
			{
				if (value == m_LayoutAvailability)
					return;

				m_LayoutAvailability = value;

				OnCallLayoutAvailabilityChanged.Raise(this, new GenericEventArgs<ZoomLayoutAvailability>(m_LayoutAvailability));
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
			OnShareThumbChanged = null;
			OnStyleChanged = null;
			OnSizeChanged = null;
			OnPositionChanged = null;
			OnSelfViewEnabledChanged = null;
			OnCallLayoutAvailabilityChanged = null;

			base.DisposeFinal();

			Unsubscribe(Parent);
		}

		#endregion

		#region Methods

		protected override void Initialize()
		{
			base.Initialize();

			GetLayoutStatus();
			UpdateLayout();
		}

		public void SetLayoutShareThumb(bool enabled)
		{
			Parent.Log(eSeverity.Informational, "Setting ShareThumb to: {0}", enabled);
			Parent.SendCommand("zConfiguration Call Layout ShareThumb: {0}", enabled ? "on" : "off");
		}

		public void SetLayoutStyle(eZoomLayoutStyle style)
		{
			Parent.Log(eSeverity.Informational, "Setting Call Layout Style to: {0}", style.ToString());
			Parent.SendCommand("zConfiguration Call Layout Style: {0}", style.ToString());
		}

		public void SetLayoutSize(eZoomLayoutSize size)
		{
			Parent.Log(eSeverity.Informational, "Setting Call Layout Size to: {0}", size.ToString());
			Parent.SendCommand("zConfiguration Call Layout Size: {0}", size.ToString());
		}

		public void SetLayoutPosition(eZoomLayoutPosition position)
		{
			if (position > eZoomLayoutPosition.DownLeft)
				throw new ArgumentOutOfRangeException("position",
				                                      "Currently only UpRight, UpLeft, DownRight, DownLeft are supported by Zoom.");

			Parent.Log(eSeverity.Informational, "Setting Call Layout Position to: {0}", position.ToString());
			Parent.SendCommand("zConfiguration Call Layout Position: {0}", position.ToString());
		}

		public void HideSelfView(bool enabled)
		{
			Parent.Log(eSeverity.Informational, "Setting Hide Self Video to: {0}", enabled);
			Parent.SendCommand("zConfiguration Video hide_conf_self_video: {0}", enabled ? "on" : "off");
		}

		public void GetLayoutStatus()
		{
			Parent.SendCommand("zStatus Call Layout");
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
			parent.RegisterResponseCallback<VideoConfigurationResponse>(VideoConfigurationResponseCallback);
			parent.RegisterResponseCallback<CallLayoutStatusResponse>(CallLayoutStatusResponseCallback);
		}

		private void Unsubscribe(ZoomRoom parent)
		{
			parent.UnregisterResponseCallback<ClientCallLayoutResponse>(ClientCallLayoutResponseCallback);
			parent.UnregisterResponseCallback<CallConfigurationResponse>(CallConfigurationCallback);
			parent.UnregisterResponseCallback<VideoConfigurationResponse>(VideoConfigurationResponseCallback);
			parent.UnregisterResponseCallback<CallLayoutStatusResponse>(CallLayoutStatusResponseCallback);
		}

		private void ClientCallLayoutResponseCallback(ZoomRoom zoomroom, ClientCallLayoutResponse response)
		{
			var topData = response.CallLayoutConfiguration;
			if (topData == null)
				return;

			var subData = topData.LayoutConfigurationHeader;
			if (subData == null)
				return;

			var data = subData.LayoutConfiguration;
			if (data == null)
				return;

			if (data.ShareThumb != null)
				ShareThumb = (bool)data.ShareThumb;
			if (data.Style != null)
				LayoutStyle = (eZoomLayoutStyle)data.Style;
			if (data.Size != null)
				LayoutSize = (eZoomLayoutSize)data.Size;
			if (data.Position != null)
				LayoutPosition = (eZoomLayoutPosition)data.Position;
		}

		private void CallConfigurationCallback(ZoomRoom zoomroom, CallConfigurationResponse response)
		{
			var topData = response.CallConfiguration;
			if (topData == null)
				return;

			var data = topData.Layout;
			if (data == null)
				return;

			if (data.Size != null)
				LayoutSize = (eZoomLayoutSize)data.Size;

			if (data.Position != null)
				LayoutPosition = (eZoomLayoutPosition)data.Position;
		}

		private void VideoConfigurationResponseCallback(ZoomRoom zoomroom, VideoConfigurationResponse response)
		{
			if (response.Video == null)
				return;

			var data = response.Video.HideConferenceSelfVideo;
			SelfViewEnabled = !data;
		}

		private void CallLayoutStatusResponseCallback(ZoomRoom zoomroom, CallLayoutStatusResponse response)
		{
			if (response == null)
				return;

			var layoutStatus = response.LayoutStatus;
			if (layoutStatus == null)
				return;

			var layoutAvailability = new ZoomLayoutAvailability
			{
				CanAdjustFloatingVideo = layoutStatus.CanAdjustFloatingVideo,
				CanSwitchFloatingShareContent = layoutStatus.CanAdjustFloatingVideo,
				CanSwitchShareOnAllScreens = layoutStatus.CanSwitchShareOnAllScreens,
				CanSwitchSpeakerView = layoutStatus.CanSwitchSpeakerView,
				CanSwitchWallView = layoutStatus.CanSwitchWallView,
				IsInFirstPage = layoutStatus.IsInFirstPage,
				IsInLastPage = layoutStatus.IsInLastPage,
				IsSupported = layoutStatus.IsSupported,
				VideoCountInCurrentPage = layoutStatus.VideoCountInCurrentPage,
				VideoType = layoutStatus.VideoType
			};

			LayoutAvailability = layoutAvailability;
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
