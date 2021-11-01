namespace ICD.Connect.Conferencing.DialContexts
{
	/// <summary>
	/// The types of protocols for booking numbers.
	/// Arranged in ascending order of least qualified to most qualified.
	/// </summary>
	public enum eDialProtocol
	{
		Unknown = 0,
		Pstn = 1,
		Sip = 2,
		Zoom = 3,
		ZoomContact = 4,
		ZoomPersonal = 5,
		Spark = 6
	}
}
