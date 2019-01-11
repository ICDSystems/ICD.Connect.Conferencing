using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.DialContexts
{
	public class ZoomContactDialContext : AbstractDialContext
	{
		public override eDialProtocol Protocol 
		{
			get { return eDialProtocol.ZoomContact; }
		}

		public override eCallType CallType
		{
			get { return eCallType.Video; }
		}
	}
}