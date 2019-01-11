using System;
using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Conferencing.Participants.EventHelpers
{
	public sealed class TraditionalParticipantEventHelper : AbstractParticipantEventHelper<ITraditionalParticipant>
	{
		public TraditionalParticipantEventHelper(Action<ITraditionalParticipant> callback) : base(callback)
		{
		}

		#region ITraditionalParticipant Callbacks

		protected override void SubscribeInternal(ITraditionalParticipant participant)
		{
			base.SubscribeInternal(participant);

			participant.OnNumberChanged += ParticipantOnNumberChanged;
		}

		protected override void UnsubscribeInternal(ITraditionalParticipant participant)
		{
			base.UnsubscribeInternal(participant);

			participant.OnNumberChanged -= ParticipantOnNumberChanged;
		}

		private void ParticipantOnNumberChanged(object sender, StringEventArgs stringEventArgs)
		{
			Callback(sender as ITraditionalParticipant);
		}

		#endregion
	}
}