using System;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.DialContexts
{
	public interface IDialContext
	{
		eDialProtocol Protocol { get; }

		eCallType CallType { get; }

		string DialString { get; }

		string Password { get; }
	}

	public static class DialContextExtensions
	{
		public static IDialContext ZoomToSipDialContext(this IDialContext context)
		{
			if (context.Protocol != eDialProtocol.Zoom)
				throw new InvalidOperationException("Must be Zoom protocol to convert to Sip");

			return new SipDialContext
			{
				DialString = "sip:" + context.DialString + "@zmus.us",
                CallType = context.CallType
			};
		} 
	}
}
