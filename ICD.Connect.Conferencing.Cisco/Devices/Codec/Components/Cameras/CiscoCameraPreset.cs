namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Cameras
{
	public struct CiscoCameraPreset
	{
		private readonly int m_CameraId;
		private readonly string m_Name;
		private readonly int m_PresetId;

		#region Properties

		/// <summary>
		/// Gets the camera id.
		/// </summary>
		public int CameraId { get { return m_CameraId; } }

		/// <summary>
		/// Gets the name.
		/// </summary>
		public string Name { get { return m_Name; } }

		/// <summary>
		/// Gets the preset id.
		/// </summary>
		public int PresetId {get { return m_PresetId; }}

		#endregion

		#region Constructors.

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="cameraId"></param>
		/// <param name="name"></param>
		/// <param name="presetId"></param>
		public CiscoCameraPreset(int cameraId, string name, int presetId)
		{
			m_CameraId = cameraId;
			m_Name = name;
			m_PresetId = presetId;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Implementing default equality
		/// </summary>
		/// <param name="s1"></param>
		/// <param name="s2"></param>
		/// <returns></returns>
		public static bool operator ==(CiscoCameraPreset s1, CiscoCameraPreset s2)
		{
			return s1.Equals(s2);
		}

		/// <summary>
		/// Implementing default inequality.
		/// </summary>
		/// <param name="s1"></param>
		/// <param name="s2"></param>
		/// <returns></returns>
		public static bool operator !=(CiscoCameraPreset s1, CiscoCameraPreset s2)
		{
			return !(s1 == s2);
		}

		/// <summary>
		/// Returns true if this instance is equal to the given object.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override bool Equals(object other)
		{
			if (other == null || GetType() != other.GetType())
				return false;

			return GetHashCode() == ((CiscoCameraPreset)other).GetHashCode();
		}

		/// <summary>
		/// Gets the hashcode for this instance.
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = hash * 23 + m_CameraId;
				hash = hash * 23 + (m_Name == null ? 0 : m_Name.GetHashCode());
				hash = hash * 23 + m_PresetId;
				return hash;
			}
		}

		#endregion
	}
}