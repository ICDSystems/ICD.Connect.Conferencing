namespace ICD.Connect.Conferencing.DialContexts
{
	public sealed class ThinDialContext : AbstractDialContext
	{
		private readonly eDialProtocol m_Protocol;

		public override eDialProtocol Protocol { get { return m_Protocol; } }

		public ThinDialContext(eDialProtocol protocol)
		{
			m_Protocol = protocol;
		}
	}
}
