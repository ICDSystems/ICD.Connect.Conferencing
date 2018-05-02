﻿using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.Controls;

namespace ICD.Connect.Conferencing.Server
{
	public interface IInterpreterBooth
	{
		event EventHandler<GenericEventArgs<IDialingDeviceControl>> OnDialerAdded;
		event EventHandler<GenericEventArgs<IDialingDeviceControl>> OnDialerRemoved;
		event EventHandler<GenericEventArgs<IDialingDeviceControl>> OnDialerChanged;

		event EventHandler<DialerSourceEventArgs> OnDialerSourceChanged;
		event EventHandler<DialerSourceEventArgs> OnDialerSourceAdded;
		event EventHandler<DialerSourceEventArgs> OnDialerSourceRemoved;

		int Id { get; }

		IEnumerable<IDialingDeviceControl> GetDialers();

		string GetLanguageForDialer(IDialingDeviceControl dialer); 

		void AddDialer(IDialingDeviceControl dialer);
		void AddDialer(IDialingDeviceControl dialer, string language);
		void RemoveDialer(IDialingDeviceControl dialer);
		void ClearDialers();
	}
}