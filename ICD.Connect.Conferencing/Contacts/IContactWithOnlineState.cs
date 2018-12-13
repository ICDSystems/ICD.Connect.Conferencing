using System;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.Contacts
{
	public interface IContactWithOnlineState
	{
		eOnlineState OnlineState { get; }

		event EventHandler<OnlineStateEventArgs> OnOnlineStateChanged;
	}
}