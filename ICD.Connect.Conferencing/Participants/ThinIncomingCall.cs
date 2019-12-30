using System;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.Participants
{
	public sealed class ThinIncomingCall : AbstractIncomingCall
	{
		public override event EventHandler<StringEventArgs> OnNameChanged;
		public override event EventHandler<StringEventArgs> OnNumberChanged;
		public override event EventHandler<IncomingCallAnswerStateEventArgs> OnAnswerStateChanged;
	}
}