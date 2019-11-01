using System;
using ICD.Connect.Conferencing.Participants;

namespace ICD.Connect.Conferencing.Conferences
{
	public sealed class TraditionalConference : AbstractConference<ITraditionalParticipant>, ITraditionalConference, IDisposable
	{
	}
}
