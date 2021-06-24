using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.Participants;
using ICD.Common.Utils.Extensions;

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

	public static class ParticipantEventArgsExtensions
	{
		/// <summary>
		/// Raises the event safely. Simply skips if the handler is null.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="sender"></param>
		/// <param name="data"></param>
		public static void Raise([CanBeNull]this EventHandler<ParticipantEventArgs> extends, object sender, IParticipant data)
		{
			extends.Raise(sender, new ParticipantEventArgs(data));
		}
	}
}
