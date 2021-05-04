using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.RoomAnalytics;
using ICD.Connect.Partitioning.Commercial.Controls.Occupancy;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Controls
{
	public sealed class CiscoCodecOccupancySensorControl : AbstractOccupancySensorControl<CiscoCodecDevice>
	{
		private static readonly BiDictionary<ePeoplePresence, eOccupancyState> s_PeoplePresenceToOccupancy =
			new BiDictionary<ePeoplePresence, eOccupancyState>
			{
				{ePeoplePresence.Unknown, eOccupancyState.Unknown},
				{ePeoplePresence.No, eOccupancyState.Unoccupied},
				{ePeoplePresence.Yes, eOccupancyState.Occupied}
			};

		private readonly RoomAnalyticsComponent m_Component;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public CiscoCodecOccupancySensorControl(CiscoCodecDevice parent, int id)
			: base(parent, id)
		{
			m_Component = parent.Components.GetComponent<RoomAnalyticsComponent>();
			Subscribe(m_Component);

			UpdateState();
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			Unsubscribe(m_Component);

			base.DisposeFinal(disposing);
		}

		#region Private Methods

		/// <summary>
		/// Updates the occupancy state to match the codec.
		/// </summary>
		private void UpdateState()
		{
			SetOccupancySupported(m_Component.PeoplePresenceDetectorEnabled);

			ePeoplePresence peoplePresence = m_Component.PeoplePresence;
			OccupancyState = s_PeoplePresenceToOccupancy.GetValue(peoplePresence);

			UpdatePeopleCount(m_Component.PeopleCountCurrent);
		}

		private void UpdatePeopleCount(int peopleCount)
		{
			// <0 means people counting isn't turned on, so update supported features
			bool supported = peopleCount >= 0;
			
			SetPeopleCountSupported(supported);
			
			PeopleCount = supported ? peopleCount : 0;
		}

		#endregion

		#region Codec Callbacks

		/// <summary>
		/// Subscribe to the component events.
		/// </summary>
		/// <param name="component"></param>
		private void Subscribe(RoomAnalyticsComponent component)
		{
			component.OnPeoplePresenceDetectorEnabledChanged += ComponentOnOnPeoplePresenceDetectorEnabledChanged;
			component.OnPeoplePresenceChanged += ComponentOnPeoplePresenceChanged;
			component.OnPeopleCountCurrentChanged += ComponentOnOnPeopleCountCurrentChanged;
		}

		/// <summary>
		/// Unsubscribe from the component events.
		/// </summary>
		/// <param name="component"></param>
		private void Unsubscribe(RoomAnalyticsComponent component)
		{
			component.OnPeoplePresenceDetectorEnabledChanged -= ComponentOnOnPeoplePresenceDetectorEnabledChanged;
			component.OnPeoplePresenceChanged -= ComponentOnPeoplePresenceChanged;
			component.OnPeopleCountCurrentChanged -= ComponentOnOnPeopleCountCurrentChanged;
		}

		private void ComponentOnOnPeoplePresenceDetectorEnabledChanged(object sender, BoolEventArgs args)
		{
			SetOccupancySupported(args.Data);
		}

		/// <summary>
		/// Called when the people presence state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void ComponentOnPeoplePresenceChanged(object sender, PeoplePresenceEventArgs eventArgs)
		{
			UpdateState();
		}

		private void ComponentOnOnPeopleCountCurrentChanged(object sender, IntEventArgs args)
		{
			UpdatePeopleCount(args.Data);
		}

		#endregion
	}
}
