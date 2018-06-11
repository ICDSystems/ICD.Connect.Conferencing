using System;
using ICD.Connect.Conferencing.Controls.Presentation;
using ICD.Connect.Conferencing.Polycom.Devices.Components.Content;

namespace ICD.Connect.Conferencing.Polycom.Devices.Controls
{
	public sealed class PolycomCodecPresentationControl : AbstractPresentationControl<PolycomGroupSeriesDevice>
	{
		private readonly ContentComponent m_ContentComponent;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public PolycomCodecPresentationControl(PolycomGroupSeriesDevice parent, int id)
			: base(parent, id)
		{
			m_ContentComponent = parent.Components.GetComponent<ContentComponent>();

			Subscribe(m_ContentComponent);
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			Unsubscribe(m_ContentComponent);
		}

		#region Methods

		/// <summary>
		/// Starts presenting the source at the given input address.
		/// </summary>
		/// <param name="input"></param>
		public override void StartPresentation(int input)
		{
			m_ContentComponent.Play(input);
		}

		/// <summary>
		/// Stops the active presentation.
		/// </summary>
		public override void StopPresentation()
		{
			m_ContentComponent.Stop();
		}

		#endregion

		#region Content Component

		/// <summary>
		/// Subscribe to the content component events.
		/// </summary>
		/// <param name="contentComponent"></param>
		private void Subscribe(ContentComponent contentComponent)
		{
			contentComponent.OnContentVideoSourceChanged += ContentComponentOnContentVideoSourceChanged;
		}

		/// <summary>
		/// Unsubscribe from the content component events.
		/// </summary>
		/// <param name="contentComponent"></param>
		private void Unsubscribe(ContentComponent contentComponent)
		{
			contentComponent.OnContentVideoSourceChanged -= ContentComponentOnContentVideoSourceChanged;
		}

		/// <summary>
		/// Called when the active content video source changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void ContentComponentOnContentVideoSourceChanged(object sender, ContentVideoSourceEventArgs eventArgs)
		{
			PresentationActiveInput = m_ContentComponent.ContentVideoSource;
		}

		#endregion
	}
}
