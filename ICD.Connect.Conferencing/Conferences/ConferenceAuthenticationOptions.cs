﻿using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.Conferences
{
    public enum eCodeRequirement
    {
        NotSupported, // Code is not supported and should not be sent
        Optional, // Code is optional
        Required // Code is required
    } 
    public sealed class ConferenceAuthenticationOptions
    {
        /// <summary>
        /// Raised when the password is rejected. Args is the message.
        /// </summary>
        public event EventHandler<StringEventArgs> OnPasswordRejected;
        
        private readonly eConferenceAuthenticationState m_State;
        private readonly bool m_IsCodeAlphanumeric;
        private readonly ConferenceAuthenticationMethod[] m_AuthenticationMethods;


        /// <summary>
        /// Authentication State for the conference
        /// </summary>
        public eConferenceAuthenticationState State
        {
            get { return m_State; }
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
                                               IEnumerable<ConferenceAuthenticationMethod> authenticationMethods)
        {
            m_State = state;
            m_IsCodeAlphanumeric = isCodeAlphanumeric;
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
        public void RaisePasswordRejected(string message)
        {
            OnPasswordRejected.Raise(this, message);
        }

    }
    
    public sealed class ConferenceAuthenticationMethod
    {
        private readonly string m_Name;
        private readonly string m_Prompt;
        private readonly eCodeRequirement m_CodeRequirement;
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
        /// Prompt to use for the authentication code text box
        /// </summary>
        [CanBeNull]
        public string Prompt
        {
            get { return m_Prompt; }
        }

        /// <summary>
        /// Code requirements for this method
        /// </summary>
        public eCodeRequirement CodeRequirement
        {
            get { return m_CodeRequirement; }
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
        /// <param name="codeRequirement"></param>
        /// <param name="authenticationCallback"></param>
        public ConferenceAuthenticationMethod([NotNull] string name, [CanBeNull] string prompt, eCodeRequirement codeRequirement, [NotNull] Action<string> authenticationCallback)
        {
            if (name == null) 
                throw new ArgumentNullException("name");
            if (authenticationCallback == null) 
                throw new ArgumentNullException("authenticationCallback");
            
            m_Name = name;
            m_Prompt = prompt;
            m_CodeRequirement = codeRequirement;
            m_AuthenticationCallback = authenticationCallback;
        }
    }
}