﻿using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.DialContexts
{
    public abstract class AbstractDialContext : IDialContext
    {
	    private eCallType m_CallType;
	    private string m_DialString;

	    public abstract eDialProtocol Protocol { get; }

	    public virtual eCallType CallType
	    {
		    get { return m_CallType; }
		    set { m_CallType = value; }
	    }

	    public virtual string DialString
	    {
		    get { return m_DialString; }
		    set { m_DialString = value; }
	    }
    }
}