using System;
using ICD.Connect.Conferencing.Participants;

namespace ICD.Connect.Conferencing.Conferences
{
	public sealed class Conference : AbstractConference<IParticipant>, IDisposable
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		public Conference()
		{
			SupportedConferenceFeatures = eConferenceFeatures.GetStatus |
			                              eConferenceFeatures.GetStartTime |
			                              eConferenceFeatures.GetEndTime |
			                              eConferenceFeatures.GetCallType |
			                              eConferenceFeatures.GetParticipants;
		}
	}
}
