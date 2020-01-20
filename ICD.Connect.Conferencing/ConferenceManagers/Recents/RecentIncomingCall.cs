using System;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.IncomingCalls;
using ICD.Connect.Conferencing.Participants;

namespace ICD.Connect.Conferencing.ConferenceManagers.Recents
{
	public sealed class RecentIncomingCall : AbstractRecentCall
	{
		private readonly IIncomingCall m_IncomingCall;

		public override string Name { get { return m_IncomingCall.Name; } }

		public override string Number { get { return m_IncomingCall.Number; } }

		public override DateTime Time { get { return m_IncomingCall.GetEndOrStartTime(); } }
		public override eCallDirection Direction { get { return m_IncomingCall.Direction; } }
		public override eCallAnswerState AnswerState { get { return m_IncomingCall.AnswerState; } }

		public override eCallType CallType
		{
			get
			{
				TraditionalIncomingCall traditional = m_IncomingCall as TraditionalIncomingCall;
				return traditional != null ? traditional.CallType : eCallType.Unknown;
			}
		}

		public IIncomingCall IncomingCall { get { return m_IncomingCall; } }

		public RecentIncomingCall(IIncomingCall incomingCall)
		{
			m_IncomingCall = incomingCall;
		}
	}
}
