﻿using System;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Presentation;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Video;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Video.Connectors;
using ICD.Connect.Conferencing.Controls.Presentation;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Controls
{
	public sealed class CiscoCodecPresentationControl : AbstractPresentationControl<CiscoCodecDevice>
	{
		public event EventHandler<PresentationModeEventArgs> OnPresentationModeChanged;

		private readonly VideoComponent m_Video;
		private readonly PresentationComponent m_Presentation;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public CiscoCodecPresentationControl(CiscoCodecDevice parent, int id)
			: base(parent, id)
		{
			m_Video = parent.Components.GetComponent<VideoComponent>();
			m_Presentation = parent.Components.GetComponent<PresentationComponent>();

			Subscribe(m_Presentation);

			UpdatePresentationActiveInput();
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			Unsubscribe(m_Presentation);
		}

		public ePresentationMode PresentationMode
		{
			get { return m_Presentation == null ? ePresentationMode.Off : m_Presentation.PresentationMode; }
		}

		#region Methods

		/// <summary>
		/// Starts presenting the source at the given input address.
		/// </summary>
		/// <param name="input"></param>
		public override void StartPresentation(int input)
		{
			VideoInputConnector connector = m_Video.GetVideoInputConnector(input);
			m_Presentation.StartPresentation(connector.SourceId, PresentationItem.eSendingMode.LocalRemote);
		}

		/// <summary>
		/// Stops the active presentation.
		/// </summary>
		public override void StopPresentation()
		{
			foreach (PresentationItem presentation in m_Presentation.GetPresentations())
				m_Presentation.StopPresentation(presentation);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Updates the presentation active input.
		/// </summary>
		private void UpdatePresentationActiveInput()
		{
			int? input = null;

			PresentationItem active;
			if (m_Presentation.GetPresentations().TryFirst(out active))
				input = active.VideoInputConnector;

			PresentationActiveInput = input;
		}

		#endregion

		#region Video Component Callbacks

		/// <summary>
		/// Subscribe to the presentation component events.
		/// </summary>
		/// <param name="presentation"></param>
		private void Subscribe(PresentationComponent presentation)
		{
			presentation.OnPresentationsChanged += PresentationOnPresentationsChanged;
			presentation.OnPresentationStopped += PresentationOnPresentationStopped;
			presentation.OnPresentationModeChanged += PresentationOnPresentationModeChanged;
		}

		/// <summary>
		/// Unsubscribe from the presentation component events.
		/// </summary>
		/// <param name="presentation"></param>
		private void Unsubscribe(PresentationComponent presentation)
		{
			presentation.OnPresentationsChanged -= PresentationOnPresentationsChanged;
			presentation.OnPresentationStopped -= PresentationOnPresentationStopped;
			presentation.OnPresentationModeChanged -= PresentationOnPresentationModeChanged;
		}

		private void PresentationOnPresentationStopped(object sender, StringEventArgs stringEventArgs)
		{
			UpdatePresentationActiveInput();
		}

		private void PresentationOnPresentationsChanged(object sender, EventArgs eventArgs)
		{
			UpdatePresentationActiveInput();
		}

		private void PresentationOnPresentationModeChanged(object sender, PresentationModeEventArgs e)
		{
			switch (e.Data)
			{
				case ePresentationMode.Receiving:
				case ePresentationMode.Sending:
					PresentationActive = true;
					break;
				case ePresentationMode.Off:
					PresentationActive = false;
					break;
			}

			UpdatePresentationActiveInput();

			OnPresentationModeChanged.Raise(this, new PresentationModeEventArgs(e.Data));
		}

		#endregion
	}
}
