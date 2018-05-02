using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Controls;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Devices;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Conferencing.Server
{
	public sealed class InterpretationAdapter : AbstractDevice<InterpretationAdapterSettings>, IInterpretationAdapter
	{

		public event EventHandler<GenericEventArgs<IDialingDeviceControl>> OnDialerAdded;
		public event EventHandler<GenericEventArgs<IDialingDeviceControl>> OnDialerRemoved;
		public event EventHandler<GenericEventArgs<IDialingDeviceControl>> OnDialerChanged;

		public event EventHandler<DialerSourceEventArgs> OnDialerSourceChanged;
		public event EventHandler<DialerSourceEventArgs> OnDialerSourceAdded;
		public event EventHandler<DialerSourceEventArgs> OnDialerSourceRemoved;

		#region Private Memebers

		private readonly Dictionary<IDialingDeviceControl, string> m_Dialers; 

		#endregion

		#region Public Properties

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public InterpretationAdapter()
		{
			m_Dialers = new Dictionary<IDialingDeviceControl, string>();
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);
		}

		#region Public Methods

		public Dictionary<IDialingDeviceControl, string> GetDialingControls()
		{
			return m_Dialers.ToDictionary();
		}

		#endregion

		#region Private Helper Methods

		#endregion

		#region Simpl Adapter

		//paramless constructor
		//set booth ushort method (called from simpl)
		

		#endregion

		#region Dialers

		private void AddDialer(IDialingDeviceControl dialer)
		{
			AddDialer(dialer, string.Empty);
		}

		private void AddDialer(IDialingDeviceControl dialer, string language)
		{
			if(m_Dialers.ContainsKey(dialer))
				return;

			m_Dialers.Add(dialer, language);
			Subscribe(dialer);
		}

		private void RemoveDialer(IDialingDeviceControl dialer)
		{
			if (!m_Dialers.ContainsKey(dialer))
				return;

			m_Dialers.Remove(dialer);
			Unsubscribe(dialer);
		}

		private void ClearDialers()
		{
			foreach (var dialer in m_Dialers.Keys)
			{
				Unsubscribe(dialer);
			}
			m_Dialers.Clear();
		}

		private void Subscribe(IDialingDeviceControl control)
		{
			if (control == null)
				return;

			control.OnPrivacyMuteChanged += DialerOnPropertyChanged;
			control.OnAutoAnswerChanged += DialerOnPropertyChanged;
			control.OnDoNotDisturbChanged += DialerOnPropertyChanged;
			control.OnSourceAdded += DialerOnSourceAdded;
			control.OnSourceRemoved += DialerOnSourceRemoved;
			control.OnSourceChanged += DialerOnSourceChanged;
		}

		private void Unsubscribe(IDialingDeviceControl control)
		{
			if (control == null)
				return;

			control.OnPrivacyMuteChanged -= DialerOnPropertyChanged;
			control.OnAutoAnswerChanged -= DialerOnPropertyChanged;
			control.OnDoNotDisturbChanged -= DialerOnPropertyChanged;
			control.OnSourceAdded -= DialerOnSourceAdded;
			control.OnSourceRemoved -= DialerOnSourceRemoved;
			control.OnSourceChanged -= DialerOnSourceChanged;
		}

		private void DialerOnPropertyChanged(object sender, EventArgs args)
		{
			OnDialerChanged.Raise(this, new GenericEventArgs<IDialingDeviceControl>(sender as IDialingDeviceControl));
		}

		private void DialerOnSourceAdded(object sender, ConferenceSourceEventArgs args)
		{
			OnDialerSourceAdded.Raise(this, new DialerSourceEventArgs(sender as IDialingDeviceControl, args.Data));
		}

		private void DialerOnSourceRemoved(object sender, ConferenceSourceEventArgs args)
		{
			OnDialerSourceRemoved.Raise(this, new DialerSourceEventArgs(sender as IDialingDeviceControl, args.Data));
		}

		private void DialerOnSourceChanged(object sender, ConferenceSourceEventArgs args)
		{
			OnDialerSourceChanged.Raise(this, new DialerSourceEventArgs(sender as IDialingDeviceControl, args.Data));
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(InterpretationAdapterSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(InterpretationAdapterSettings settings)
		{
			base.CopySettingsFinal(settings);
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();
		}

		#endregion

		#region Console

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);
		}

		#endregion

		#region IDevice

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
