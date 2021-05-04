using ICD.Connect.API.EventArguments;
using ICD.Connect.Conferencing.Participants;

namespace ICD.Connect.Conferencing.EventArguments
{
	public sealed class ConferenceParticipantSupportedFeaturesChangedApiEventArgs :
		AbstractGenericApiEventArgs<eParticipantFeatures>
	{
		public ConferenceParticipantSupportedFeaturesChangedApiEventArgs(eParticipantFeatures data)
			: base("OnSupportedParticipantFeaturesChanged", data)
		{
		}
	}
}