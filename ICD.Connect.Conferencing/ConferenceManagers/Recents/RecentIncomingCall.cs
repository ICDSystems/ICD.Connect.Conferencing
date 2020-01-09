using System;
using ICD.Connect.Conferencing.Participants;

namespace ICD.Connect.Conferencing.ConferenceManagers.Recents
{
	public sealed class RecentIncomingCall : AbstractRecentCall
	{
		private readonly IIncomingCall m_IncomingCall;

		public override string Name { get { return m_IncomingCall.Name; } }

		public override string Number { get { return m_IncomingCall.Number; } }

		public override DateTime Time { get { return m_IncomingCall.GetEndOrStartTime(); } }

		public IIncomingCall IncomingCall { get { return m_IncomingCall; } }

		public RecentIncomingCall(IIncomingCall incomingCall)
		{
			m_IncomingCall = incomingCall;
		}
	}
}
