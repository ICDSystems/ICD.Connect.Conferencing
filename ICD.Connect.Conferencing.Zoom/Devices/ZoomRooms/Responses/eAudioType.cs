namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses
{
	public enum eAudioType
	{
		/// <summary>
		/// Connected only with video
		/// </summary>
		AUDIO_NONE,
		/// <summary>
		/// Connected over internet
		/// </summary>
		AUDIO_VOIP,
		/// <summary>
		/// Connected via PSTN
		/// </summary>
		AUDIO_TELE
	}
}