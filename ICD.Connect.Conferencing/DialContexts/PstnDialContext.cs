namespace ICD.Connect.Conferencing.DialContexts
{
	public sealed class PstnDialContext : AbstractDialContext
	{
		public override eDialProtocol Protocol 
		{
			get { return eDialProtocol.Pstn; }
		}
	}
}