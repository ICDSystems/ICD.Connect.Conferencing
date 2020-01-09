using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.Participants
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
