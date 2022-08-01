using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Conferences;

namespace ICD.Connect.Conferencing.EventArguments
{
    public enum eConferenceAuthenticationState
    {
        /// <summary>
        /// Authentication not required or available
        /// </summary>
        None,
        
        /// <summary>
        /// Authentication Optional, but not required
        /// </summary>
        Optional,
        
        /// <summary>
        /// Some response to authentication is required
        /// </summary>
        Required
    }
    public sealed class ConferenceAuthenticationOptionsEventArgs : GenericEventArgs<ConferenceAuthenticationOptions>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="data"></param>
        public ConferenceAuthenticationOptionsEventArgs(ConferenceAuthenticationOptions data) : base(data)
        { }
    }
    
    public static class ConferenceAuthenticationOptionsEventArgsExtensions
    {
        /// <summary>
        /// Raises the event safely. Simply skips if the handler is null.
        /// </summary>
        /// <param name="extends"></param>
        /// <param name="sender"></param>
        /// <param name="data"></param>
        public static void Raise([CanBeNull]this EventHandler<ConferenceAuthenticationOptionsEventArgs> extends, object sender, ConferenceAuthenticationOptions data)
        {
            extends.Raise(sender, new ConferenceAuthenticationOptionsEventArgs(data));
        }
    }
}