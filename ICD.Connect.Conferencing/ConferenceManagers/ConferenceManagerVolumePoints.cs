using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Connect.Audio.VolumePoints;

namespace ICD.Connect.Conferencing.ConferenceManagers
{
	public sealed class ConferenceManagerVolumePoints : IEnumerable<IVolumePoint>
	{
		private readonly SafeCriticalSection m_PointsSection;
		private readonly IcdHashSet<IVolumePoint> m_Points; 

		/// <summary>
		/// Constructor.
		/// </summary>
		public ConferenceManagerVolumePoints()
		{
			m_PointsSection = new SafeCriticalSection();
			m_Points = new IcdHashSet<IVolumePoint>();
		}

		#region Methods

		/// <summary>
		/// Deregisters all of the registered volume points.
		/// </summary>
		public void Clear()
		{
			m_PointsSection.Enter();

			try
			{
				foreach (IVolumePoint point in GetVolumePoints())
					DeregisterVolumePoint(point);
			}
			finally
			{
				m_PointsSection.Leave();
			}
		}

		/// <summary>
		/// Gets the registered volume points.
		/// </summary>
		/// <returns></returns>
		[NotNull]
		public IEnumerable<IVolumePoint> GetVolumePoints()
		{
			return m_PointsSection.Execute(() => m_Points.ToArray());
		}

		/// <summary>
		/// Registers the volume point.
		/// </summary>
		/// <param name="volumePoint"></param>
		public bool RegisterVolumePoint([NotNull] IVolumePoint volumePoint)
		{
			if (volumePoint == null)
				throw new ArgumentNullException("volumePoint");

			return m_PointsSection.Execute(() => m_Points.Add(volumePoint));
		}

		/// <summary>
		/// Deregisters the volume point.
		/// </summary>
		/// <param name="volumePoint"></param>
		public bool DeregisterVolumePoint([NotNull] IVolumePoint volumePoint)
		{
			if (volumePoint == null)
				throw new ArgumentNullException("volumePoint");

			return m_PointsSection.Execute(() => m_Points.Remove(volumePoint));
		}

		#endregion

		#region Enumerable

		public IEnumerator<IVolumePoint> GetEnumerator()
		{
			return GetVolumePoints().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion
	}
}
