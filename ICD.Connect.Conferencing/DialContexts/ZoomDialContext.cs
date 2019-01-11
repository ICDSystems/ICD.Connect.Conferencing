namespace ICD.Connect.Conferencing.DialContexts
{
	public sealed class ZoomDialContext : AbstractDialContext
	{
		public override eDialProtocol Protocol
		{
			get { return eDialProtocol.Zoom; }
		}
	}
}