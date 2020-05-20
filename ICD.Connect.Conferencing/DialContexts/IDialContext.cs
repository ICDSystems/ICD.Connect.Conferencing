using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.DialContexts
{
	public interface IDialContext
	{
		/// <summary>
		/// Gets the protocol for placing the call.
		/// </summary>
		eDialProtocol Protocol { get; }

		/// <summary>
		/// Gets the type of call.
		/// </summary>
		eCallType CallType { get; }

		/// <summary>
		/// Gets the number, uri, etc for placing the call.
		/// </summary>
		string DialString { get; }

		/// <summary>
		/// Gets the password for joining the call.
		/// </summary>
		string Password { get; }
	}
}
