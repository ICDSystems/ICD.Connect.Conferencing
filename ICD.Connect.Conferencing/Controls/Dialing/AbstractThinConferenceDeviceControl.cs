using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.IncomingCalls;
using ICD.Connect.Devices;

namespace ICD.Connect.Conferencing.Controls.Dialing
{
	public abstract class AbstractThinConferenceDeviceControl<T> : AbstractConferenceDeviceControl<T,ThinConference> where T : IDevice
	{
		#region Events

		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallAdded;

		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallRemoved;

		/// <summary>
		/// Raised when a conference is added to the dialing control.
		/// </summary>
		public override event EventHandler<ConferenceEventArgs> OnConferenceAdded;

		/// <summary>
		/// Raised when a conference is removed from the dialing control.
		/// </summary>
		public override event EventHandler<ConferenceEventArgs> OnConferenceRemoved;

		#endregion

		#region Fields

		private readonly SafeCriticalSection m_IncomingCallsSection;
		private readonly IcdHashSet<IIncomingCall> m_IncomingCalls; 

		private readonly SafeCriticalSection m_ConferencesSection;
		private readonly IcdHashSet<ThinConference> m_Conferences;

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		protected AbstractThinConferenceDeviceControl(T parent, int id) : base(parent, id)
		{
			m_IncomingCalls = new IcdHashSet<IIncomingCall>();
			m_IncomingCallsSection = new SafeCriticalSection();
			m_Conferences = new IcdHashSet<ThinConference>();
			m_ConferencesSection = new SafeCriticalSection();
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		/// <param name="uuid"></param>
		protected AbstractThinConferenceDeviceControl(T parent, int id, Guid uuid) : base(parent, id, uuid)
		{
			m_IncomingCalls = new IcdHashSet<IIncomingCall>();
			m_IncomingCallsSection = new SafeCriticalSection();
			m_Conferences = new IcdHashSet<ThinConference>();
			m_ConferencesSection = new SafeCriticalSection();
		}

		#endregion

		/// <summary>
		/// Gets the active conference sources.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<ThinConference> GetConferences()
		{
			return m_ConferencesSection.Execute(() => m_Conferences.ToArray(m_Conferences.Count));
		}

		protected IEnumerable<IIncomingCall> GetIncomingCalls()
		{
			return m_IncomingCallsSection.Execute(() => m_IncomingCalls.ToArray(m_IncomingCalls.Count));
		}

		#region Protected Methods

		protected bool AddIncomingCall(IIncomingCall call)
		{
			bool added;

			m_IncomingCallsSection.Enter();
			try
			{
				added = m_IncomingCalls.Add(call);
			}
			finally
			{
				m_IncomingCallsSection.Leave();
			}

			if (added)
				OnIncomingCallAdded.Raise(this, call);

			return added;
		}

		protected bool RemoveIncomingCall(IIncomingCall call)
		{
			bool removed;

			m_IncomingCallsSection.Enter();
			try
			{
				removed = m_IncomingCalls.Remove(call);
			}
			finally
			{
				m_IncomingCallsSection.Leave();
			}

			if (removed)
				OnIncomingCallRemoved.Raise(this, call);

			return removed;
		}

		protected bool AddConference(ThinConference conference)
		{
			bool added;
			
			m_ConferencesSection.Enter();
			try
			{
				added = m_Conferences.Add(conference);
			}
			finally
			{
				m_ConferencesSection.Leave();
			}

			if (added)
				OnConferenceAdded.Raise(this, conference);

			return added;
		}

		protected bool RemoveConference(ThinConference conference)
		{
			bool removed;
			
			m_ConferencesSection.Enter();
			try
			{
				removed = m_Conferences.Remove(conference);
			}
			finally
			{
				m_ConferencesSection.Leave();
			}

			if (removed)
				OnConferenceRemoved.Raise(this, conference);

			return removed;
		}

		#endregion
	}
}