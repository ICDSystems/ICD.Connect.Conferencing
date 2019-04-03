using System;
using System.Linq;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Telemetry;

namespace ICD.Connect.Conferencing.Controls.Dialing
{
	public sealed class DialingDeviceExternalTelemetryProvider : IDialingDeviceExternalTelemetryProvider
	{
		public event EventHandler OnRequestTelemetryRebuild;
		public event EventHandler<BoolEventArgs> OnCallInProgressChanged;
		public event EventHandler<StringEventArgs> OnCallTypeChanged;
		public event EventHandler<StringEventArgs> OnCallNumberChanged;

		private IDialingDeviceControl m_Parent;

		public void SetParent(ITelemetryProvider provider)
		{
			if(!(provider is IDialingDeviceControl))
				throw new InvalidOperationException(
					string.Format("Cannot create external telemetry for provider {0}, " +
					              "Provider must be of type IDialingDeviceControl.", provider));

			if (m_Parent != null)
			{
				m_Parent.OnSourceAdded -= ParentOnSourceAddedOrRemoved;
				m_Parent.OnSourceRemoved -= ParentOnSourceAddedOrRemoved;
				m_Parent.OnSourceChanged -= ParentOnSourceAddedOrRemoved;
			}

			m_Parent = (IDialingDeviceControl)provider;

			if (m_Parent != null)
			{
				m_Parent.OnSourceAdded += ParentOnSourceAddedOrRemoved;
				m_Parent.OnSourceRemoved += ParentOnSourceAddedOrRemoved;
				m_Parent.OnSourceChanged += ParentOnSourceAddedOrRemoved;
			}
		}

		private void ParentOnSourceAddedOrRemoved(object sender, ConferenceSourceEventArgs conferenceSourceEventArgs)
		{
			OnCallInProgressChanged.Raise(this, new BoolEventArgs(CallInProgress));
			OnCallTypeChanged.Raise(this, new StringEventArgs(CallTypes));
			OnCallNumberChanged.Raise(this, new StringEventArgs(CallNumbers));
		}

		/// <summary>
		/// Gets whether the dialing device has a call in progress
		/// </summary>
		public bool CallInProgress 
		{
			get
			{
				if (m_Parent == null)
					return false;

				return m_Parent.GetSources().Any();
			} 
		}

		/// <summary>
		/// Gets a comma separated list of the type of each active call on the dialer.
		/// </summary>
		public string CallTypes 
		{
			get
			{
				if (m_Parent == null)
					return null;

				return string.Join(", ", m_Parent.GetSources().Select(s => s.SourceType.ToString()).ToArray());
			} 
		}

		/// <summary>
		/// Gets a comma separated list of the type of each active call on the dialer.
		/// </summary>
		public string CallNumbers 
		{
			get
			{
				if (m_Parent == null)
					return null;

				return string.Join(", ", m_Parent.GetSources().Select(s => s.Number).ToArray());
			} 
		}

		/// <summary>
		/// Forces all calls on the dialer to end.
		/// </summary>
		public void EndCalls()
		{
			if (m_Parent == null)
				return;
			foreach (IConferenceSource source in m_Parent.GetSources())
			{
				source.Hangup();
			}
		}
	}
}