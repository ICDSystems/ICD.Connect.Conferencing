using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Conferencing.Cameras;

namespace ICD.Connect.Conferencing.Cisco.Components.Cameras
{
	/// <summary>
	/// FarCamera provides functionality for controlling a remote camera.
	/// </summary>
	public sealed class FarCamera : AbstractCamera
	{
		/// <summary>
		/// The CallId for the remote camera.
		/// </summary>
		private readonly int m_CallId;

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public override string ConsoleHelp { get { return "Far camera for call id: " + m_CallId; } }

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="callId"></param>
		/// <param name="codec"></param>
		public FarCamera(int callId, CiscoCodec codec) : base(codec)
		{
			m_CallId = callId;

			Subscribe(Codec);

			if (Codec.Initialized)
				Initialize();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Moves the camera.
		/// </summary>
		/// <param name="action"></param>
		public override void Move(eCameraAction action)
		{
			Codec.SendCommand("xCommand Call FarEndControl Camera Move CallId: {0} Value: {1}", m_CallId, action);
			Codec.Log(eSeverity.Informational, "Moving Far End Camera CallId: {0}, Direction: {1}", m_CallId, action);
		}

		/// <summary>
		/// Stops the camera from moving.
		/// </summary>
		public override void Stop()
		{
			Codec.SendCommand("xCommand Call FarEndControl Camera Stop CallId: {0}", m_CallId);
			Codec.Log(eSeverity.Informational, "Stop Moving Far End Camera CallId: {0}", m_CallId);
		}

		#endregion
	}
}
