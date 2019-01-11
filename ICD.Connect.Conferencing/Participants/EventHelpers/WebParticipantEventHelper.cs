using System;
using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Conferencing.Participants.EventHelpers
{
	public sealed class WebParticipantEventHelper : AbstractParticipantEventHelper<IWebParticipant>
	{
		public WebParticipantEventHelper(Action<IWebParticipant> callback)
			: base(callback)
		{
		}
	}
}