using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;

namespace ICD.Connect.Conferencing.Conferences
{
	/// <summary>
	/// Conference States
	/// </summary>
	public enum eConferenceStatus
	{
		Undefined,
		Connecting,
		Connected,
		Disconnecting,
		Disconnected,
// ReSharper disable InconsistentNaming
		OnHold
// ReSharper restore InconsistentNaming
	}

	public sealed class ConferenceStatusEventArgs : GenericEventArgs<eConferenceStatus>
	{
		/// <summary>
		/// Primary Constructor
		/// </summary>
		/// <param name="state"></param>
		public ConferenceStatusEventArgs(eConferenceStatus state) : base(state)
		{
		}
	}

	public static class ConferenceStatusEventArgsExtensions
	{
		/// <summary>
		/// Raises the event safely. Simply skips if the handler is null.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="sender"></param>
		/// <param name="data"></param>
		public static void Raise([CanBeNull]this EventHandler<ConferenceStatusEventArgs> extends, object sender, eConferenceStatus data)
		{
			extends.Raise(sender, new ConferenceStatusEventArgs(data));
		}
	}
}
