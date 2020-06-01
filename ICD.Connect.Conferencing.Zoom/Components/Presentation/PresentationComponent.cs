using System;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Conferencing.Zoom.Responses;
using ICD.Common.Logging.LoggingContexts;

namespace ICD.Connect.Conferencing.Zoom.Components.Presentation
{
	public sealed class PresentationComponent : AbstractZoomRoomComponent
	{
		public event EventHandler<BoolEventArgs> OnInputConnectedUpdated;
		public event EventHandler<BoolEventArgs> OnSignalDetectedUpdated;
		public event EventHandler<GenericEventArgs<eSharingState>> OnSharingStateChanged;
		public event EventHandler<PresentationOutputEventArgs> OnPresentationOutputChanged;

		private bool m_InputConnected;
		private bool m_SignalDetected;
		private eSharingState m_SharingState;
		private int? m_ShareOutput;

		#region Properties

		public bool InputConnected
		{
			get { return m_InputConnected; }
			set
			{
				if (m_InputConnected == value)
					return;

				m_InputConnected = value;

				Parent.Logger.LogSetTo(eSeverity.Informational, "InputConnected", m_InputConnected);

				OnInputConnectedUpdated.Raise(this, new BoolEventArgs(m_InputConnected));
			}
		}

		public bool SignalDetected
		{
			get { return m_SignalDetected; }
			set
			{
				if (m_SignalDetected == value)
					return;

				m_SignalDetected = value;

				Parent.Logger.LogSetTo(eSeverity.Informational, "SignalDetected", m_SignalDetected);

				OnSignalDetectedUpdated.Raise(this, new BoolEventArgs(m_SignalDetected));
			}
		}

		public eSharingState SharingState
		{
			get { return m_SharingState; }
			private set
			{
				if (m_SharingState == value)
					return;

				m_SharingState = value;

				Parent.Logger.LogSetTo(eSeverity.Informational, "SharingState", m_SharingState);

				OnSharingStateChanged.Raise(this, new GenericEventArgs<eSharingState>(m_SharingState));
			}
		}

		public int? PresentationOutput
		{
			get { return m_ShareOutput; }
			private set
			{
				if (m_ShareOutput == value)
					return;

				m_ShareOutput = value;

				Parent.Logger.LogSetTo(eSeverity.Informational, "PresentationOutput", m_ShareOutput);

				OnPresentationOutputChanged.Raise(this, new PresentationOutputEventArgs(m_ShareOutput));
			}
		}

		#endregion

		public PresentationComponent(ZoomRoom parent)
			: base(parent)
		{
			Subscribe(parent);
		}

		protected override void DisposeFinal()
		{
			OnInputConnectedUpdated = null;
			OnSignalDetectedUpdated = null;
			OnSharingStateChanged = null;
			OnPresentationOutputChanged = null;

			base.DisposeFinal();

			Unsubscribe(Parent);
		}

		#region Methods

		public void StartPresentation()
		{
			if (!InputConnected)
			{
				Parent.Logger.Log(eSeverity.Error, "Unable to start HDMI share - BlackMagic is not connected");
				return;
			}

			Parent.Logger.Log(eSeverity.Informational, "Starting HDMI share");
			Parent.SendCommand("zCommand Call Sharing HDMI Start");
		}

		public void StopPresentation()
		{
			Parent.Logger.Log(eSeverity.Informational, "Stopping HDMI share");
			Parent.SendCommand("zCommand Call Sharing HDMI Stop");
		}

		protected override void Initialize()
		{
			base.Initialize();
	
			Parent.SendCommand("zStatus Sharing");
		}

		#endregion

		#region Zoom Room Callbacks

		private void Subscribe(ZoomRoom parent)
		{
			parent.RegisterResponseCallback<SharingResponse>(SharingResponseCallback);
			parent.RegisterResponseCallback<SharingStateResponse>(SharingStateResponseCallback);
			parent.RegisterResponseCallback<PinStatusOfScreenNotificationResponse>(PinStatusCallback);
		}

		private void Unsubscribe(ZoomRoom parent)
		{
			parent.UnregisterResponseCallback<SharingResponse>(SharingResponseCallback);
			parent.UnregisterResponseCallback<SharingStateResponse>(SharingStateResponseCallback);
			parent.UnregisterResponseCallback<PinStatusOfScreenNotificationResponse>(PinStatusCallback);
		}

		private void SharingResponseCallback(ZoomRoom zoomRoom, SharingResponse response)
		{
			if (response.Sharing == null)
				return;

			InputConnected = response.Sharing.IsBlackMagicConnected;
			SignalDetected = response.Sharing.IsBlackMagicDataAvailable;
		}

		private void SharingStateResponseCallback(ZoomRoom zoomroom, SharingStateResponse response)
		{
			if (response.SharingState == null)
				return;

			SharingState = response.SharingState.State;
		}

		private void PinStatusCallback(ZoomRoom zoomRoom, PinStatusOfScreenNotificationResponse response)
		{
			PinStatusOfScreenNotification data = response.PinStatusOfScreenNotification;
			if (data == null)
				return;

			if (data.ScreenLayout == eZoomScreenLayout.ShareContent)
				PresentationOutput = data.ScreenIndex;
			else if (m_ShareOutput == data.ScreenIndex)
				PresentationOutput = null;
		}

		#endregion
	}
}