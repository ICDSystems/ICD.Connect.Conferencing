using System;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Video
{
	/// <summary>
	/// Simple pairing of video connector id to connection state.
	/// </summary>
	public sealed class VideoConnectionStateEventArgs : EventArgs
	{
		private readonly int m_Id;
		private readonly bool m_State;

		/// <summary>
		/// Gets the video connector id.
		/// </summary>
		public int Id { get { return m_Id; } }

		/// <summary>
		/// Gets the connection state.
		/// </summary>
		public bool State { get { return m_State; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="state"></param>
		public VideoConnectionStateEventArgs(int id, bool state)
		{
			m_Id = id;
			m_State = state;
		}
	}
}
