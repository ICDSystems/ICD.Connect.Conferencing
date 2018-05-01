using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Controls;

namespace ICD.Connect.Conferencing.Server
{
	public sealed class InterpreterBooth : IInterpreterBooth
	{
		public event EventHandler<GenericEventArgs<IDialingDeviceControl>> OnDialerAdded;
		public event EventHandler<GenericEventArgs<IDialingDeviceControl>> OnDialerRemoved;

		private readonly List<IDialingDeviceControl> m_Dialers;
		private readonly Dictionary<IDialingDeviceControl, string> m_Languages; 
		
		public int Id { get; private set; }

		public InterpreterBooth(int id)
		{
			Id = id;
			m_Dialers = new List<IDialingDeviceControl>();
			m_Languages = new Dictionary<IDialingDeviceControl, string>();
		}

		public IEnumerable<IDialingDeviceControl> GetDialers()
		{
			return m_Dialers.ToArray();
		}

		public void AddDialer(IDialingDeviceControl dialer)
		{
			if (m_Dialers.Contains(dialer))
				return;

			m_Dialers.Add(dialer);

			OnDialerAdded.Raise(this, new GenericEventArgs<IDialingDeviceControl>(dialer));
		}

		public void AddDialer(IDialingDeviceControl dialer, string language)
		{
			m_Languages.Add(dialer, language);
			AddDialer(dialer);
		}

		public void RemoveDialer(IDialingDeviceControl dialer)
		{
			if(!m_Dialers.Contains(dialer))
				return;

			if (m_Languages.ContainsKey(dialer))
				m_Languages.Remove(dialer);

			m_Dialers.Remove(dialer);

			OnDialerRemoved.Raise(this, new GenericEventArgs<IDialingDeviceControl>(dialer));
		}

		public void ClearDialers()
		{
			foreach (IDialingDeviceControl dialer in m_Dialers.ToArray())
			{
				RemoveDialer(dialer);
			}
		}

		public string GetLanguageForDialer(IDialingDeviceControl dialer)
		{
			return !m_Languages.ContainsKey(dialer) ? string.Empty : m_Languages[dialer];
		}
	}
}