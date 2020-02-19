using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;
using ICD.Connect.Devices;

namespace ICD.Connect.Conferencing.Controls.Dialing
{
	public abstract class AbstractTraditionalConferenceDeviceControl<T> : AbstractConferenceDeviceControl<T, ITraditionalConference>, ITraditionalConferenceDeviceControl
		where T : IDeviceBase
	{
		/// <summary>
		/// Raised when a source is added to the conference component.
		/// </summary>
		public override event EventHandler<ConferenceEventArgs> OnConferenceAdded;

		/// <summary>
		/// Raised when a source is removed from the conference component.
		/// </summary>
		public override event EventHandler<ConferenceEventArgs> OnConferenceRemoved;

		private TraditionalConference m_ActiveConference;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		protected AbstractTraditionalConferenceDeviceControl(T parent, int id)
			: base(parent, id)
		{
		}

		#region Methods

		/// <summary>
		/// Gets the active conference sources.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<ITraditionalConference> GetConferences()
		{
			if (m_ActiveConference != null)
				yield return m_ActiveConference;
		}

		#endregion

		#region Source Events

		protected void AddParticipant(ITraditionalParticipant participant)
		{
			if (m_ActiveConference == null)
			{
				m_ActiveConference = new TraditionalConference();
				OnConferenceAdded.Raise(this, new ConferenceEventArgs(m_ActiveConference));
			}
			
			m_ActiveConference.AddParticipant(participant);
		}

		protected void RemoveParticipant(ITraditionalParticipant participant)
		{
			if (m_ActiveConference == null)
				return;

			m_ActiveConference.RemoveParticipant(participant);

			if (!m_ActiveConference.GetParticipants().Any())
			{
				OnConferenceRemoved.Raise(this, new ConferenceEventArgs(m_ActiveConference));
				m_ActiveConference = null;
			}
		}

		#endregion
	}
}
