using System;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Presentation;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Proximity;
using ICD.Connect.Conferencing.Controls.DirectSharing;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Controls
{
	public sealed class CiscoCodecDirectSharingControl : AbstractDirectSharingControl<CiscoCodecDevice>
	{
		#region Constants

		private const int DIRECT_SHARE_INPUT = 10;
		private const string DIRECT_SHARE_SOURCE_NAME = "Cisco Proximity";

		#endregion

		#region Fields

		private readonly PresentationComponent m_Presentation;
		private readonly ProximityComponent m_Proximity;

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public CiscoCodecDirectSharingControl([NotNull] CiscoCodecDevice parent, int id)
			: base(parent, id)
		{
			m_Presentation = parent.Components.GetComponent<PresentationComponent>();
			m_Proximity = parent.Components.GetComponent<ProximityComponent>();

			// Cisco does not have a code for direct sharing.
			SharingCode = null;

			SharingSourceName = DIRECT_SHARE_SOURCE_NAME;

			Subscribe(m_Presentation);
			Subscribe(m_Proximity);
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			Unsubscribe(m_Presentation);
			Unsubscribe(m_Proximity);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Updates the direct share active state by checking
		/// if there are any presentations active on the direct share input
		/// </summary>
		private void UpdateDirectShareActive()
		{
			PresentationItem[] presentations = m_Presentation.GetPresentations();
			DirectShareActive = presentations.Any(p => p.VideoInputConnector == DIRECT_SHARE_INPUT);
		}

		#endregion

		#region Presentation Component Callbacks

		/// <summary>
		/// Subscribe to the events of the codec presentation component.
		/// </summary>
		/// <param name="presentation"></param>
		private void Subscribe(PresentationComponent presentation)
		{
			presentation.OnPresentationsChanged += PresentationOnPresentationsChanged;
			presentation.OnPresentationStopped += PresentationOnPresentationStopped;
		}

		/// <summary>
		/// Unsubscribe to the events of the codec presentation component.
		/// </summary>
		/// <param name="presentation"></param>
		private void Unsubscribe(PresentationComponent presentation)
		{
			presentation.OnPresentationsChanged -= PresentationOnPresentationsChanged;
			presentation.OnPresentationStopped -= PresentationOnPresentationStopped;
		}

		private void PresentationOnPresentationsChanged(object sender, EventArgs args)
		{
			UpdateDirectShareActive();
		}

		private void PresentationOnPresentationStopped(object sender, StringEventArgs args)
		{
			UpdateDirectShareActive();
		}

		#endregion

		#region Proximity Component Callbacks

		/// <summary>
		/// Subscribe to the events of the codec proximity component.
		/// </summary>
		/// <param name="proximity"></param>
		private void Subscribe(ProximityComponent proximity)
		{
			proximity.OnProximityModeChanged += ProximityOnProximityModeChanged;
		}

		/// <summary>
		/// Unsubscribe from the events of the codec proximity component.
		/// </summary>
		/// <param name="proximity"></param>
		private void Unsubscribe(ProximityComponent proximity)
		{
			proximity.OnProximityModeChanged -= ProximityOnProximityModeChanged;
		}

		private void ProximityOnProximityModeChanged(object sender, ProximityModeEventArgs args)
		{
			DirectShareEnabled = args.Data == eProximityMode.On;
		}

		#endregion
	}
}
