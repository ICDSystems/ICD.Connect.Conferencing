using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.IncomingCalls;
using ICD.Connect.Conferencing.Participants.Enums;

namespace ICD.Connect.Conferencing.ConferenceManagers.History
{
	public sealed class HistoricalIncomingParticipant : IHistoricalParticipant
	{

		private IIncomingCall m_IncomingCall;

		public string Name { get; private set; }

		public string Number { get; private set; }

		public DateTime? StartTime { get; private set; }
		public DateTime? EndTime { get; private set; }
		public eCallDirection Direction { get { return eCallDirection.Incoming; } }
		public eCallAnswerState AnswerState { get; private set; }

		public eCallType CallType { get; private set; }

		public HistoricalIncomingParticipant([NotNull] IIncomingCall incomingCall)
		{
			if (incomingCall == null)
				throw new ArgumentNullException("incomingCall");

			Subscribe(incomingCall);
			UpdateCall(incomingCall);
		}

		public void Detach()
		{
			Unsubscribe(m_IncomingCall);
			m_IncomingCall = null;
		}

		private void UpdateCall(IIncomingCall incomingCall)
		{
			Name = incomingCall.Name;
			Number = incomingCall.Number;
			StartTime = incomingCall.GetEndOrStartTime();
			AnswerState = incomingCall.AnswerState;

			TraditionalIncomingCall traditional = incomingCall as TraditionalIncomingCall;
			CallType = traditional != null ? traditional.CallType : eCallType.Unknown;
		}

		#region IncomingCall Callbacks

		private void Subscribe(IIncomingCall incomingCall)
		{
			if (incomingCall == null)
				return;

			incomingCall.OnNameChanged += IncomingCallOnOnNameChanged;
			incomingCall.OnNumberChanged += IncomingCallOnOnNumberChanged;
			incomingCall.OnAnswerStateChanged += IncomingCallOnOnAnswerStateChanged;
		}

		private void Unsubscribe(IIncomingCall incomingCall)
		{
			if (incomingCall == null)
				return;

			incomingCall.OnNameChanged -= IncomingCallOnOnNameChanged;
			incomingCall.OnNumberChanged -= IncomingCallOnOnNumberChanged;
			incomingCall.OnAnswerStateChanged -= IncomingCallOnOnAnswerStateChanged;
		}

		private void IncomingCallOnOnNameChanged(object sender, StringEventArgs args)
		{
			Name = args.Data;
		}

		private void IncomingCallOnOnNumberChanged(object sender, StringEventArgs args)
		{
			Number = args.Data;
		}

		private void IncomingCallOnOnAnswerStateChanged(object sender, CallAnswerStateEventArgs args)
		{
			AnswerState = args.Data;
		}

		#endregion
	}
}
