using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.Controls.Layout;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Zoom.Components.Call;
using ICD.Connect.Conferencing.Zoom.Components.Layout;
using ICD.Connect.Conferencing.Zoom.Responses;

namespace ICD.Connect.Conferencing.Zoom.Controls
{
	public sealed class ZoomRoomLayoutControl : AbstractConferenceLayoutControl<ZoomRoom>
	{
		private readonly LayoutComponent m_LayoutComponent;
		private readonly CallComponent m_CallComponent;

		#region Constructor

		public ZoomRoomLayoutControl(ZoomRoom parent, int id) 
			: base(parent, id)
		{
			m_LayoutComponent = Parent.Components.GetComponent<LayoutComponent>();
			m_CallComponent = Parent.Components.GetComponent<CallComponent>();

			Subscribe(m_LayoutComponent);
			Subscribe(m_CallComponent);

			LayoutAvailable = true;
		}

		protected override void DisposeFinal(bool disposing)
		{
			Unsubscribe(m_LayoutComponent);
			Unsubscribe(m_CallComponent);

			LayoutAvailable = false;

			base.DisposeFinal(disposing);
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

		public void SetShareThumb(bool enabled)
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
			m_LayoutComponent.UpdateLayout();
		}

		#endregion

		#region Zoom Room Callbacks

		protected override void Subscribe(ZoomRoom parent)
		{
			base.Subscribe(parent);

			parent.RegisterResponseCallback<VideoConfigurationResponse>(VideoConfigurationResponseCallback);;
		}

		protected override void Unsubscribe(ZoomRoom parent)
		{
			base.Unsubscribe(parent);

			parent.UnregisterResponseCallback<VideoConfigurationResponse>(VideoConfigurationResponseCallback);
		}

		private void VideoConfigurationResponseCallback(ZoomRoom zoomroom, VideoConfigurationResponse response)
		{
			if (response.Video == null)
				return;

			var data = response.Video.HideConferenceSelfVideo;
			SelfViewEnabled = !data;
		}

		#endregion

		#region Layout Component Callbacks

		private void Subscribe(LayoutComponent layoutComponent)
		{
			layoutComponent.OnSizeChanged += LayoutComponentOnSizeChanged;
		}

		private void Unsubscribe(LayoutComponent layoutComponent)
		{
			layoutComponent.OnSizeChanged -= LayoutComponentOnSizeChanged;
		}

		private void LayoutComponentOnSizeChanged(object sender, LayoutConfigurationEventArgs e)
		{
			SelfViewEnabled = e.LayoutSize != eZoomLayoutSize.Off;
		}

		#endregion

		#region Call Component Callbacks

		private void Subscribe(CallComponent callComponent)
		{
			callComponent.OnStatusChanged += CallComponentOnStatusChanged;
			callComponent.OnParticipantRemoved += CallComponentOnParticipantRemoved;
		}

		private void Unsubscribe(CallComponent callComponent)
		{
			callComponent.OnStatusChanged -= CallComponentOnStatusChanged;
			callComponent.OnParticipantRemoved -= CallComponentOnParticipantRemoved;
		}

		private void CallComponentOnStatusChanged(object sender, ConferenceStatusEventArgs args)
		{
			UpdateLayout();
		}

		private void CallComponentOnParticipantRemoved(object sender, ParticipantEventArgs e)
		{
			if (m_CallComponent.GetParticipants().Count() <= 1)
				SelfViewEnabled = true;

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
