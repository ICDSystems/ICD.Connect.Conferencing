using System;
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

			UpdatePresentationActive();
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
		/// Updates the presentation active state.
		/// </summary>
		private void UpdatePresentationActive()
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
		}

		/// <summary>
		/// Unsubscribe from the presentation component events.
		/// </summary>
		/// <param name="presentation"></param>
		private void Unsubscribe(PresentationComponent presentation)
		{
			presentation.OnPresentationsChanged -= PresentationOnPresentationsChanged;
			presentation.OnPresentationStopped -= PresentationOnPresentationStopped;
		}

		private void PresentationOnPresentationStopped(object sender, StringEventArgs stringEventArgs)
		{
			UpdatePresentationActive();
		}

		private void PresentationOnPresentationsChanged(object sender, EventArgs eventArgs)
		{
			UpdatePresentationActive();
		}

		#endregion
	}
}
