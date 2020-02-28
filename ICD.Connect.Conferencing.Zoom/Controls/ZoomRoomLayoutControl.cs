using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Controls.Layout;
using ICD.Connect.Conferencing.Zoom.Components.Call;
using ICD.Connect.Conferencing.Zoom.Components.Layout;
using ICD.Connect.Conferencing.Zoom.Responses;

namespace ICD.Connect.Conferencing.Zoom.Controls
{
	public sealed class ZoomRoomLayoutControl : AbstractConferenceLayoutControl<ZoomRoom>
	{
		/// <summary>
		/// Raised when the share thumbnail enabled state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnShareThumbnailEnabledStateChanged;

		private readonly LayoutComponent m_LayoutComponent;
		private readonly CallComponent m_CallComponent;
		private bool m_ShareThumbnailEnabled;

		/// <summary>
		/// Returns true if the share thumbnail is enabled.
		/// </summary>
		public bool ShareThumbnailEnabled
		{
			get { return m_ShareThumbnailEnabled; }
			private set
			{
				if (value == m_ShareThumbnailEnabled)
					return;

				m_ShareThumbnailEnabled = value;

				OnShareThumbnailEnabledStateChanged.Raise(this, new BoolEventArgs(m_ShareThumbnailEnabled));
			}
		}

		#region Constructor

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public ZoomRoomLayoutControl(ZoomRoom parent, int id) 
			: base(parent, id)
		{
			m_LayoutComponent = Parent.Components.GetComponent<LayoutComponent>();
			m_CallComponent = Parent.Components.GetComponent<CallComponent>();

			Subscribe(m_LayoutComponent);
			Subscribe(m_CallComponent);

			LayoutAvailable = true;
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnShareThumbnailEnabledStateChanged = null;

			base.DisposeFinal(disposing);

			Unsubscribe(m_LayoutComponent);
			Unsubscribe(m_CallComponent);

			LayoutAvailable = false;
		}

		#endregion

		#region Methods

		public override void SetSelfViewEnabled(bool enabled)
		{
			if (m_CallComponent.GetParticipants().Count() <= 1)
			{
				Parent.Log(eSeverity.Error, "Can not hide self view with 1 or less participants");
				return;
			}

			m_LayoutComponent.HideSelfView(!enabled);
		}

		public override void SetSelfViewFullScreenEnabled(bool enabled)
		{
			throw new System.NotSupportedException();
		}

		public override void SetLayoutMode(eLayoutMode mode)
		{
			throw new System.NotSupportedException();
		}

		public void SetShareThumbnailEnabled(bool enabled)
		{
			m_LayoutComponent.SetLayoutShareThumb(enabled);
		}

		public void SetStyle(eZoomLayoutStyle style)
		{
			m_LayoutComponent.SetLayoutStyle(style);
		}

		public void SetLayoutSize(eZoomLayoutSize size)
		{
			m_LayoutComponent.SetLayoutSize(size);
		}

		public void SetLayoutPosition(eZoomLayoutPosition position)
		{
			m_LayoutComponent.SetLayoutPosition(position);
		}

		#endregion

		#region Private Methods

		private void UpdateLayout()
		{
			if (m_CallComponent.GetParticipants().Count() <= 1)
				SelfViewEnabled = true;

			m_LayoutComponent.GetLayoutStatus();
			m_LayoutComponent.UpdateLayout();
		}

		#endregion

		#region Layout Component Callbacks

		private void Subscribe(LayoutComponent layoutComponent)
		{
			layoutComponent.OnSizeChanged += LayoutComponentOnSizeChanged;
			layoutComponent.OnSelfViewEnabledChanged += LayoutComponentOnSelfViewEnabledChanged;
			layoutComponent.OnShareThumbChanged += LayoutComponentOnShareThumbChanged;
		}

		private void Unsubscribe(LayoutComponent layoutComponent)
		{
			layoutComponent.OnSizeChanged -= LayoutComponentOnSizeChanged;
			layoutComponent.OnSelfViewEnabledChanged -= LayoutComponentOnSelfViewEnabledChanged;
			layoutComponent.OnShareThumbChanged -= LayoutComponentOnShareThumbChanged;
		}

		private void LayoutComponentOnShareThumbChanged(object sender, BoolEventArgs eventArgs)
		{
			ShareThumbnailEnabled = m_LayoutComponent.ShareThumb;
		}

		private void LayoutComponentOnSizeChanged(object sender, ZoomLayoutSizeEventArgs args)
		{
			UpdateSelfViewEnabled();
		}

		private void LayoutComponentOnSelfViewEnabledChanged(object sender, BoolEventArgs boolEventArgs)
		{
			UpdateSelfViewEnabled();
		}

		private void UpdateSelfViewEnabled()
		{
			SelfViewEnabled =
				m_LayoutComponent.SelfViewEnabled &&
				m_LayoutComponent.LayoutSize != eZoomLayoutSize.Off;
		}

		#endregion
		#region Call Component Callbacks

		private void Subscribe(CallComponent callComponent)
		{
			callComponent.OnStatusChanged += CallComponentOnStatusChanged;
			callComponent.OnParticipantAdded += CallComponentOnParticipantAdded;
			callComponent.OnParticipantRemoved += CallComponentOnParticipantRemoved;
		}

		private void Unsubscribe(CallComponent callComponent)
		{
			callComponent.OnStatusChanged -= CallComponentOnStatusChanged;
			callComponent.OnParticipantAdded -= CallComponentOnParticipantAdded;
			callComponent.OnParticipantRemoved -= CallComponentOnParticipantRemoved;
		}

		private void CallComponentOnStatusChanged(object sender, GenericEventArgs<eCallStatus> eventArgs)
		{
			UpdateLayout();
		}

		private void CallComponentOnParticipantAdded(object sender, GenericEventArgs<ParticipantInfo> genericEventArgs)
		{
			UpdateLayout();
		}

		private void CallComponentOnParticipantRemoved(object sender, GenericEventArgs<ParticipantInfo> eventArgs)
		{
			UpdateLayout();	
		}

		#endregion

		#region Console

		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			addRow("Layout Available", LayoutAvailable);
			addRow("Self-View Enabled", SelfViewEnabled);
			addRow("ShareThumb", m_LayoutComponent.ShareThumb);
			addRow("Style", m_LayoutComponent.LayoutStyle);
			addRow("Size", m_LayoutComponent.LayoutSize);
			addRow("Position", m_LayoutComponent.LayoutPosition);
		}

		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield return new GenericConsoleCommand<bool>("SetSelfViewEnabled", "SetSelfViewEnabled <true/false>",
			                                             b => SetSelfViewEnabled(b));

			yield return new GenericConsoleCommand<bool>("SetShareThumb", "SetShareThumb <true/false>",
			                                             b => m_LayoutComponent.SetLayoutShareThumb(b));
			yield return new GenericConsoleCommand<eZoomLayoutStyle>("SetStyle",
			                                                         "SetStyle <Gallery, Speaker, Strip, ShareAll>",
			                                                         e => m_LayoutComponent.SetLayoutStyle(e));
			yield return new GenericConsoleCommand<eZoomLayoutSize>("SetSize",
			                                                        "SetSize <Off, Size1, Size2, Size3, Strip>",
			                                                        e => m_LayoutComponent.SetLayoutSize(e));
			yield return new GenericConsoleCommand<eZoomLayoutPosition>("SetPosition",
			                                                            "SetPosition <UpRight, DownRight, UpLeft, DownLeft>",
			                                                            e => m_LayoutComponent.SetLayoutPosition(e));
		}

		#endregion
	}
}
