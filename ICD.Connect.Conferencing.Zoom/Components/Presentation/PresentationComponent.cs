﻿using System;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Conferencing.Zoom.Responses;

namespace ICD.Connect.Conferencing.Zoom.Components.Presentation
{
	public sealed class PresentationComponent : AbstractZoomRoomComponent
	{
		public event EventHandler<BoolEventArgs> OnInputConnectedUpdated;
		public event EventHandler<BoolEventArgs> OnSignalDetectedUpdated;
		public event EventHandler<BoolEventArgs> OnLocalSharingChanged;
		public event EventHandler<PresentationOutputEventArgs> OnPresentationOutputChanged;

		private bool m_InputConnected;
		private bool m_SignalDetected;
		private bool m_Sharing;
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
				Parent.Log(eSeverity.Informational, "InputConnected changed to: {0}", m_InputConnected);
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
				Parent.Log(eSeverity.Informational, "SignalDetected changed to: {0}", m_SignalDetected);
				OnSignalDetectedUpdated.Raise(this, new BoolEventArgs(m_SignalDetected));
			}
		}

		public bool Sharing
		{
			get { return m_Sharing; }
			private set
			{
				if (m_Sharing == value)
					return;

				m_Sharing = value;
				Parent.Log(eSeverity.Informational, "Sharing changed to: {0}", m_Sharing);
				OnLocalSharingChanged.Raise(this, new BoolEventArgs(m_Sharing));
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
				Parent.Log(eSeverity.Informational, "PresentationOutput changed to: {0}", m_ShareOutput);
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
			OnLocalSharingChanged = null;
			OnPresentationOutputChanged = null;

			base.DisposeFinal();

			Unsubscribe(Parent);
		}

		#region Methods

		public void StartPresentation()
		{
			if (!InputConnected)
			{
				Parent.Log(eSeverity.Error, "Unable to start HDMI share - BlackMagic is not connected");
				return;
			}

			Parent.Log(eSeverity.Informational, "Starting HDMI share");
			Parent.SendCommand("zCommand Call Sharing HDMI Start");
		}

		public void StopPresentation()
		{
			Parent.Log(eSeverity.Informational, "Stopping HDMI share");
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
			parent.RegisterResponseCallback<PinStatusOfScreenNotificationResponse>(PinStatusCallback);
		}

		private void Unsubscribe(ZoomRoom parent)
		{
			parent.UnregisterResponseCallback<SharingResponse>(SharingResponseCallback);
			parent.UnregisterResponseCallback<PinStatusOfScreenNotificationResponse>(PinStatusCallback);
		}

		private void SharingResponseCallback(ZoomRoom zoomRoom, SharingResponse response)
		{
			if (response.Sharing == null)
				return;

			InputConnected = response.Sharing.IsBlackMagicConnected;
			SignalDetected = response.Sharing.IsBlackMagicDataAvailable;
			Sharing = response.Sharing.IsSharingBlackMagic;
		}

		private void PinStatusCallback(ZoomRoom zoomRoom, PinStatusOfScreenNotificationResponse response)
		{
			var data = response.PinStatusOfScreenNotification;
			if (data.ScreenLayout == eZoomScreenLayout.ShareContent)
				PresentationOutput = data.ScreenIndex;
			else if (m_ShareOutput == data.ScreenIndex)
				PresentationOutput = null;
		}

		#endregion
	}
}