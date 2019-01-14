namespace ICD.Connect.Conferencing.DialContexts
{
	public sealed class SipDialContext : AbstractDialContext
	{
		public override eDialProtocol Protocol
		{
			get { return eDialProtocol.Sip; }
		}
	}
}