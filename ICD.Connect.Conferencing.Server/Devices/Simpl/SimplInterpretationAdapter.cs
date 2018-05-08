using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Devices.Simpl;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Conferencing.Server.Devices.Simpl
{
	public sealed class SimplInterpretationAdapter : AbstractSimplDevice<InterpretationAdapterSettings>, IInterpretationAdapter
	{
		public event EventHandler<ConferenceSourceEventArgs> OnSourceAdded;
		public event EventHandler<ConferenceSourceEventArgs> OnSourceRemoved;

		public event EventHandler<BoolEventArgs> OnAutoAnswerChanged;
		public event EventHandler<BoolEventArgs> OnDoNotDisturbChanged;
		public event EventHandler<BoolEventArgs> OnPrivacyMuteChanged;

		#region Private Members

		private IConferenceSource m_Source;
		private bool m_AutoAnswer;
		private bool m_DoNotDisturb;
		private bool m_PrivacyMute;

		#endregion

		#region Public Properties

		public string Language { get; set; }
		public ushort BoothId { get; set; }

		public bool AutoAnswer
		{
			get { return m_AutoAnswer; }
			set
			{
				if (m_AutoAnswer == value)
					return;

				m_AutoAnswer = value;

				OnAutoAnswerChanged.Raise(this, new BoolEventArgs(m_AutoAnswer));
			}
		}
		public bool DoNotDisturb
		{
			get { return m_DoNotDisturb; }
			set
			{
				if (m_DoNotDisturb == value)
					return;

				m_DoNotDisturb = value;

				OnDoNotDisturbChanged.Raise(this, new BoolEventArgs(m_DoNotDisturb));
			}
		}
		public bool PrivacyMute
		{
			get { return m_PrivacyMute; }
			set
			{
				if (m_PrivacyMute == value)
					return;

				m_PrivacyMute = value;

				OnPrivacyMuteChanged.Raise(this, new BoolEventArgs(m_PrivacyMute));
			}
		}

		#endregion

		#region Callbacks

		public SimplDialerDialCallback DialCallback { get; set; }
		public SimplDialerDialTypeCallback DialTypeCallback { get; set; }
		public SimplDialerSetAutoAnswerCallback SetAutoAnswerCallback { get; set; }
		public SimplDialerSetDoNotDisturbCallback SetDoNotDisturbCallback { get; set; }
		public SimplDialerSetPrivacyMuteCallback SetPrivacyMuteCallback { get; set; }

		public SimplDialerAnswerCallback AnswerCallback { get; set; }
		public SimplDialerSetHoldStateCallback SetHoldStateCallback { get; set; }
		public SimplDialerSendDtmfCallback SendDtmfCallback { get; set; }
		public SimplDialerEndCallCallback EndCallCallback { get; set; }

		#endregion

		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			m_Source = null;

			DialCallback = null;
			DialTypeCallback = null;
			SetAutoAnswerCallback = null;
			SetDoNotDisturbCallback = null;
			SetPrivacyMuteCallback = null;

			OnSourceAdded = null;
			OnSourceRemoved = null;
		}

		#region Public Methods

		public void Dial(string number)
		{
			SimplDialerDialCallback handler = DialCallback;
			if (handler != null)
				handler(this, number);
		}

		public void Dial(string number, eConferenceSourceType type)
		{
			SimplDialerDialTypeCallback handler = DialTypeCallback;
			if (handler != null)
				handler(this, number, type.ToUShort());
		}

		public void SetAutoAnswer(bool enabled)
		{
			SimplDialerSetAutoAnswerCallback handler = SetAutoAnswerCallback;
			if (handler != null)
				handler(this, enabled.ToUShort());
		}

		public void SetDoNotDisturb(bool enabled)
		{
			SimplDialerSetDoNotDisturbCallback handler = SetDoNotDisturbCallback;
			if (handler != null)
				handler(this, enabled.ToUShort());
		}

		public void SetPrivacyMute(bool enabled)
		{
			SimplDialerSetPrivacyMuteCallback handler = SetPrivacyMuteCallback;
			if (handler != null)
				handler(this, enabled.ToUShort());
		}

		public void Answer(IConferenceSource source)
		{
			if (m_Source != source)
				return;

			SimplDialerAnswerCallback handler = AnswerCallback;
			if (handler != null)
				handler(this);
		}

		public void SetHold(IConferenceSource source, bool enabled)
		{
			if (m_Source != source)
				return;

			SimplDialerSetHoldStateCallback handler = SetHoldStateCallback;
			if (handler != null)
				handler(this, enabled.ToUShort());
		}

		public void SendDtmf(IConferenceSource source, string data)
		{
			if (m_Source != source)
				return;

			SimplDialerSendDtmfCallback handler = SendDtmfCallback;
			if (handler != null)
				handler(this, data);
		}

		public void EndCall(IConferenceSource source)
		{
			if (m_Source != source)
				return;

			SimplDialerEndCallCallback handler = EndCallCallback;
			if (handler != null)
				handler(this);
		}

		public void AddShimSource(IConferenceSource source)
		{
			if (source == m_Source)
				return;

			m_Source = source;

			OnSourceAdded.Raise(this, new ConferenceSourceEventArgs(source));
		}

		public void RemoveShimSource(IConferenceSource source)
		{
			if (source != m_Source)
				return;

			m_Source = null;

			OnSourceRemoved.Raise(this, new ConferenceSourceEventArgs(source));
		}

		public IEnumerable<IConferenceSource> GetSources()
		{
			yield return m_Source;
		}

		public bool ContainsSource(IConferenceSource source)
		{
			return source == m_Source;
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
