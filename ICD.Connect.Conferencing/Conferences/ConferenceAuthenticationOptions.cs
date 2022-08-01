using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.Conferences
{
    public sealed class ConferenceAuthenticationOptions
    {
        public event EventHandler OnPasswordRejected;
        
        private readonly eConferenceAuthenticationState m_State;
        private readonly bool m_IsCodeAlphanumeric;
        private readonly Action m_AnonymousAuthenticationCallback;
        private readonly ConferenceAuthenticationMethod[] m_AuthenticationMethods;
        private string m_AnonymousAuthenticationName;

        /// <summary>
        /// Authentication State for the conference
        /// </summary>
        public eConferenceAuthenticationState State
        {
            get { return m_State; }
        }

        /// <summary>
        /// If true, anonymous authentication is possible
        /// If false, a code is required
        /// </summary>
        public bool IsCodeRequired
        {
            get { return m_AnonymousAuthenticationCallback == null; }
        }

        /// <summary>
        /// If true, code can include letters
        /// if false, code can only be numbers
        /// </summary>
        public bool IsCodeAlphanumeric
        {
            get { return m_IsCodeAlphanumeric; }
        }

        /// <summary>
        /// Callback to perform anonymous authentication 
        /// </summary>
        [CanBeNull]
        public Action AnonymousAuthenticationCallback
        {
            get { return m_AnonymousAuthenticationCallback; }
        }

        /// <summary>
        /// Name of the anonymous authentication method
        /// </summary>
        public string AnonymousAuthenticationName
        {
            get { return m_AnonymousAuthenticationName; }
        }
        
        /// <summary>
        /// Collection of authentication methods for the conference
        /// </summary>
        [NotNull]
        public IEnumerable<ConferenceAuthenticationMethod> AuthenticationMethods
        {
            get { return m_AuthenticationMethods; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="state"></param>
        /// <param name="isCodeAlphanumeric"></param>
        /// <param name="authenticationMethods"></param>
        public ConferenceAuthenticationOptions(eConferenceAuthenticationState state, bool isCodeAlphanumeric,
                                               IEnumerable<ConferenceAuthenticationMethod> authenticationMethods) :
            this(state, isCodeAlphanumeric, null, null, authenticationMethods)
        { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="state"></param>
        /// <param name="isCodeAlphanumeric"></param>
        /// <param name="anonymousAuthenticationCallback"></param>
        /// <param name="anonymousAuthenticationName"></param>
        /// <param name="authenticationMethods"></param>
        public ConferenceAuthenticationOptions(eConferenceAuthenticationState state, bool isCodeAlphanumeric,
                                               Action anonymousAuthenticationCallback,
                                               string anonymousAuthenticationName,
                                               IEnumerable<ConferenceAuthenticationMethod> authenticationMethods)
        {
            m_State = state;
            m_IsCodeAlphanumeric = isCodeAlphanumeric;
            m_AnonymousAuthenticationCallback = anonymousAuthenticationCallback;
            m_AnonymousAuthenticationName = anonymousAuthenticationName;
            m_AuthenticationMethods = authenticationMethods.ToArray();
        }

        /// <summary>
        /// Create a AuthenticationOption with State of None
        /// </summary>
        /// <returns></returns>
        public static ConferenceAuthenticationOptions AuthenticationStateNone()
        {
            return new ConferenceAuthenticationOptions(eConferenceAuthenticationState.None, false,
                Enumerable.Empty<ConferenceAuthenticationMethod>());
        }

        /// <summary>
        /// Raise the OnPasswordRejected event
        /// </summary>
        public void RaisePasswordRejected()
        {
            OnPasswordRejected.Raise(this);
        }

    }
    
    public sealed class ConferenceAuthenticationMethod
    {
        private readonly string m_Name;
        private readonly string m_Prompt;
        private readonly Action<string> m_AuthenticationCallback;
        
        /// <summary>
        /// Name of the authentication method/option
        /// </summary>
        [NotNull]
        public string Name
        {
            get { return m_Name; }
        }

        /// <summary>
        /// Prompt to use for the authentication request
        /// </summary>
        [CanBeNull]
        public string Prompt
        {
            get { return m_Prompt; }
        }
        
        /// <summary>
        /// Callback for the authentication method, parameter is the code
        /// </summary>
        [NotNull]
        public Action<string> AuthenticationCallback
        {
            get { return m_AuthenticationCallback; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="prompt"></param>
        /// <param name="authenticationCallback"></param>
        public ConferenceAuthenticationMethod([NotNull] string name, [CanBeNull] string prompt, [NotNull] Action<string> authenticationCallback)
        {
            if (name == null) 
                throw new ArgumentNullException("name");
            if (authenticationCallback == null) 
                throw new ArgumentNullException("authenticationCallback");
            
            m_Name = name;
            m_Prompt = prompt;
            m_AuthenticationCallback = authenticationCallback;
        }
    }
}