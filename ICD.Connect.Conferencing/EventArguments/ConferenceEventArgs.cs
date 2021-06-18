using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Conferences;

namespace ICD.Connect.Conferencing.EventArguments
{
	public sealed class ConferenceEventArgs : GenericEventArgs<IConference>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="conference"></param>
		public ConferenceEventArgs(IConference conference)
			: base(conference)
		{
		}
	}

	public static class ConferenceEventArgsExtensions
	{
		/// <summary>
		/// Raises the event safely. Simply skips if the handler is null.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="sender"></param>
		/// <param name="data"></param>
		public static void Raise([CanBeNull]this EventHandler<ConferenceEventArgs> extends, object sender, IConference data)
		{
			extends.Raise(sender, new ConferenceEventArgs(data));
		}
	}
}
