using ICD.Common.Utils;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.DialContexts
{
    public abstract class AbstractDialContext : IDialContext
    {
	    /// <summary>
	    /// Gets the protocol for placing the call.
	    /// </summary>
	    public virtual eDialProtocol Protocol { get; set; }

	    /// <summary>
	    /// Gets the type of call.
	    /// </summary>
	    public virtual eCallType CallType { get; set; }

	    /// <summary>
	    /// Gets the number, uri, etc for placing the call.
	    /// </summary>
	    public virtual string DialString { get; set; }

	    /// <summary>
	    /// Gets the password for joining the call.
	    /// </summary>
	    public virtual string Password { get; set; }

		/// <summary>
		/// Gets the string representation for this instance.
		/// </summary>
		/// <returns></returns>
	    public override string ToString()
	    {
		    ReprBuilder builder = new ReprBuilder(this);
		    {
			    if (Protocol != eDialProtocol.Unknown)
				    builder.AppendProperty("Protocol", Protocol);

			    if (CallType != eCallType.Unknown)
				    builder.AppendProperty("CallType", CallType);

			    if (!string.IsNullOrEmpty(DialString))
				    builder.AppendProperty("DialString", DialString);

			    if (!string.IsNullOrEmpty(Password))
				    builder.AppendProperty("Password", Password);
		    }
		    return builder.ToString();
	    }
    }
}
