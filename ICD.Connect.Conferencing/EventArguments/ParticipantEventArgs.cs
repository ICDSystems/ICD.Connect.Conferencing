using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.Participants;

namespace ICD.Connect.Conferencing.EventArguments
{
	public sealed class ParticipantEventArgs : GenericEventArgs<IParticipant>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="source"></param>
		public ParticipantEventArgs(IParticipant source)
			: base(source)
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="other"></param>
		public ParticipantEventArgs(ParticipantEventArgs other)
			: this(other.Data)
		{
		}
	}
}
