using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.Conferences
{
	public sealed class Conference : IConference, IDisposable
	{
		/// <summary>
		/// Called when the conference status changes.
		/// </summary>
		public event EventHandler<ConferenceStatusEventArgs> OnStatusChanged;

		public event EventHandler OnSourcesChanged;

		private readonly IcdHashSet<IConferenceSource> m_Sources;
		private readonly SafeCriticalSection m_SourcesSection;

		private eConferenceStatus m_Status;

		/// <summary>
		/// Maps source status to conference status.
		/// </summary>
		private static readonly Dictionary<eConferenceSourceStatus, eConferenceStatus> s_StatusMap =
			new Dictionary<eConferenceSourceStatus, eConferenceStatus>
			{
				{eConferenceSourceStatus.Undefined, eConferenceStatus.Undefined},
				{eConferenceSourceStatus.Dialing, eConferenceStatus.Connecting},
				{eConferenceSourceStatus.Ringing, eConferenceStatus.Connecting},
				{eConferenceSourceStatus.Connecting, eConferenceStatus.Connecting},
				{eConferenceSourceStatus.Connected, eConferenceStatus.Connected},
				{eConferenceSourceStatus.EarlyMedia, eConferenceStatus.Connected},
				{eConferenceSourceStatus.Preserved, eConferenceStatus.Connected},
				{eConferenceSourceStatus.RemotePreserved, eConferenceStatus.Connected},
				{eConferenceSourceStatus.OnHold, eConferenceStatus.OnHold},
				{eConferenceSourceStatus.Disconnecting, eConferenceStatus.Disconnected},
				{eConferenceSourceStatus.Idle, eConferenceStatus.Disconnected},
				{eConferenceSourceStatus.Disconnected, eConferenceStatus.Disconnected},
			};

		#region Properties

		/// <summary>
		/// Current conference status.
		/// </summary>
		public eConferenceStatus Status
		{
			get { return m_Status; }
			private set
			{
				if (value == m_Status)
					return;

				m_Status = value;

				OnStatusChanged.Raise(this, new ConferenceStatusEventArgs(m_Status));
			}
		}

		/// <summary>
		/// The time the conference started.
		/// </summary>
		public DateTime? Start
		{
			get
			{
				DateTime start;
				if (m_Sources.Select(s => s.Start)
				             .OfType<DateTime>()
				             .Order()
				             .TryFirst(out start))
					return start;

				return null;
			}
		}

		/// <summary>
		/// The time the conference ended.
		/// </summary>
		public DateTime? End
		{
			get
			{
				DateTime?[] ends = m_Sources.Select(s => s.End).ToArray();

				// Conference hasn't ended yet.
				if (ends.Length == 0 || ends.Any(e => e == null))
					return null;

				return ends.ExceptNulls().Max();
			}
		}

		/// <summary>
		/// Gets the number of sources in the conference.
		/// </summary>
		public int SourcesCount { get { return m_SourcesSection.Execute(() => m_Sources.Count); } }

		/// <summary>
		/// Gets the number of online sources in the conference.
		/// </summary>
		public int OnlineSourcesCount
		{
			get { return m_SourcesSection.Execute(() => m_Sources.Count(s => s.GetIsOnline())); }
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		public Conference()
		{
			m_Sources = new IcdHashSet<IConferenceSource>();
			m_SourcesSection = new SafeCriticalSection();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Clears the sources from the conference.
		/// </summary>
		public void Clear()
		{
			foreach (IConferenceSource source in GetSources())
				RemoveSource(source);
		}

		/// <summary>
		/// Adds the source to the conference.
		/// </summary>
		/// <param name="source"></param>
		/// <returns>False if the source is already in the conference.</returns>
		public bool AddSource(IConferenceSource source)
		{
			m_SourcesSection.Enter();

			try
			{
				if (!m_Sources.Add(source))
					return false;

				Subscribe(source);
			}
			finally
			{
				m_SourcesSection.Leave();
			}

			UpdateStatus();

			OnSourcesChanged.Raise(this);

			return true;
		}

		/// <summary>
		/// Removes the source from the conference.
		/// </summary>
		/// <param name="source"></param>
		/// <returns>False if the source is not in the conference.</returns>
		public bool RemoveSource(IConferenceSource source)
		{
			m_SourcesSection.Enter();

			try
			{
				if (!m_Sources.Remove(source))
					return false;

				Unsubscribe(source);
			}
			finally
			{
				m_SourcesSection.Leave();
			}

			UpdateStatus();

			OnSourcesChanged.Raise(this);

			return true;
		}

		/// <summary>
		/// Gets the sources in this conference.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConferenceSource> GetSources()
		{
			return m_SourcesSection.Execute(() => m_Sources.ToArray());
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			OnStatusChanged = null;
			OnSourcesChanged = null;

			Clear();
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Subscribes to the source events.
		/// </summary>
		/// <param name="source"></param>
		private void Subscribe(IConferenceSource source)
		{
			if (source == null)
				return;

			source.OnStatusChanged += SourceOnStatusChanged;
		}

		/// <summary>
		/// Unsubscribes from the source events.
		/// </summary>
		/// <param name="source"></param>
		private void Unsubscribe(IConferenceSource source)
		{
			if (source == null)
				return;

			source.OnStatusChanged -= SourceOnStatusChanged;
		}

		/// <summary>
		/// Called when a source status changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void SourceOnStatusChanged(object sender, ConferenceSourceStatusEventArgs args)
		{
			UpdateStatus();
		}

		private void UpdateStatus()
		{
			Status = GetStatusFromSources();
		}

		private eConferenceStatus GetStatusFromSources()
		{
			IEnumerable<eConferenceStatus> enumerable = GetSources().Select(s => s_StatusMap[s.Status]);
			IcdHashSet<eConferenceStatus> statuses = new IcdHashSet<eConferenceStatus>(enumerable);

			// All statuses are the same
			if (statuses.Count == 1)
				return statuses.First();

			// If someone is connected then the conference is connected.
			if (statuses.Contains(eConferenceStatus.Connected))
				return eConferenceStatus.Connected;

			// If someone is on hold everyone else is on hold (or connecting)
			if (statuses.Contains(eConferenceStatus.OnHold))
				return eConferenceStatus.OnHold;
			if (statuses.Contains(eConferenceStatus.Connecting))
				return eConferenceStatus.Connecting;

			// If we don't know the current state, we shouldn't assume we've disconnected.
			return eConferenceStatus.Undefined;
		}

		#endregion
	}
}
