using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.IncomingCalls
{
	public sealed class TraditionalIncomingCall : AbstractIncomingCall
	{
		public eCallType CallType { get; private set; }

		public TraditionalIncomingCall(eCallType type)
		{
			CallType = type;
		}
	}
}
