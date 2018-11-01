namespace ICD.Connect.Conferencing.DialContexts
{
	/// <summary>
	/// The types of protocols for booking numbers.
	/// Arranged in ascending order of least qualified to most qualified.
	/// </summary>
	public enum eDialProtocol
	{
		None = 0,
		Unknown = 1,
		Pstn = 2,
		Sip = 3,
		Zoom = 4,
		ZoomContact = 5
	}
}