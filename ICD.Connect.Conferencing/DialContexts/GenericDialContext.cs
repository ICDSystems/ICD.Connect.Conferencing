namespace ICD.Connect.Conferencing.DialContexts
{
	/// <summary>
	/// Represents a dial context which is either sip or pstn, but it doesn't really matter.
	/// </summary>
	public class GenericDialContext : AbstractDialContext
	{
		public override eDialProtocol Protocol
		{
			get { return eDialProtocol.Unknown; }
		}
	}
}