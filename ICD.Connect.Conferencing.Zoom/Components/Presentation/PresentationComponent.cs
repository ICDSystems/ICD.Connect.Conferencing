using System;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Zoom.Responses;

namespace ICD.Connect.Conferencing.Zoom.Components.Presentation
{
	public sealed class PresentationComponent : AbstractZoomRoomComponent
	{
		public event EventHandler<BoolEventArgs> OnSharingChanged;

		private bool m_Sharing;

		public bool Sharing
		{
			get { return m_Sharing; }
			private set
			{
				if (m_Sharing == value)
					return;
				m_Sharing = value;
				OnSharingChanged.Raise(this, new BoolEventArgs(value));
			}
		}

		public PresentationComponent(ZoomRoom parent) : base(parent)
		{
		}

		#region Methods

		public void StartPresentation()
		{
			if (!Sharing)
				Parent.SendCommand("zCommand Call Sharing HDMI Start");
		}

		public void StopPresentation()
		{
			if (Sharing)
				Parent.SendCommand("zCommand Call Sharing HDMI Stop");
		}

		#endregion

		#region Zoom Room Callbacks

		private void Subscribe(ZoomRoom parent)
		{
			Parent.OnInitializedChanged += ParentOnOnInitializedChanged;

			Parent.RegisterResponseCallback<SharingResponse>(SharingResponseCallback);
		}

		private void ParentOnOnInitializedChanged(object sender, BoolEventArgs boolEventArgs)
		{
			Parent.SendCommand("zStatus Sharing");
		}

		private void SharingResponseCallback(ZoomRoom zoomRoom, SharingResponse response)
		{
			if (response.Sharing == null)
				return;

			Sharing = response.Sharing.IsSharingBlackMagic.ToBool();
		}

		#endregion
	}
}