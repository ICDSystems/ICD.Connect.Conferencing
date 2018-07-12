using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Devices.Simpl;
using ICD.Connect.Settings.SPlusShims.EventArguments;

namespace ICD.Connect.Conferencing.Server.Devices.Simpl
{
	public sealed class SimplInterpretationDevice : AbstractSimplDevice<SimplInterpretationDeviceSettings>, ISimplInterpretationDevice
	{
		public event EventHandler<ConferenceSourceEventArgs> OnSourceAdded;
		public event EventHandler<ConferenceSourceEventArgs> OnSourceRemoved;

		public event EventHandler<SPlusBoolEventArgs> OnAutoAnswerChanged;
		public event EventHandler<SPlusBoolEventArgs> OnDoNotDisturbChanged;
		public event EventHandler<SPlusBoolEventArgs> OnPrivacyMuteChanged;

		public event EventHandler<SPlusUShortEventArgs> OnBoothIdChanged;
		public event EventHandler<SPlusStringEventArgs> OnLanguageChanged;

		#region Private Members

		private IConferenceSource m_Source;
		private bool m_AutoAnswer;
		private bool m_DoNotDisturb;
		private bool m_PrivacyMute;
		private ushort m_BoothId;
		private string m_Language;

		#endregion

		#region Public Properties

		public string Language
		{
			get { return m_Language; }
			set
			{
				if (m_Language == value)
					return;

				m_Language = value;

				OnLanguageChanged.Raise(this, new SPlusStringEventArgs(m_Language));
			}
		}

		public ushort BoothId 
		{ 
			get { return m_BoothId; }
			set
			{
				if(m_BoothId == value)
					return;

				m_BoothId = value;

				OnBoothIdChanged.Raise(this, new SPlusUShortEventArgs(m_BoothId));
			} 
		}

		public bool AutoAnswer
		{
			get { return m_AutoAnswer; }
			set
			{
				if (m_AutoAnswer == value)
					return;

				m_AutoAnswer = value;

				OnAutoAnswerChanged.Raise(this, new SPlusBoolEventArgs(m_AutoAnswer));
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

				OnDoNotDisturbChanged.Raise(this, new SPlusBoolEventArgs(m_DoNotDisturb));
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

				OnPrivacyMuteChanged.Raise(this, new SPlusBoolEventArgs(m_PrivacyMute));
			}
		}

		#endregion

		#region Callbacks

		public SimplDialerDialCallback DialCallback { get; set; }
		public SimplDialerDialTypeCallback DialTypeCallback { get; set; }
		public SimplDialerSetAutoAnswerCallback SetAutoAnswerCallback { get; set; }
		public SimplDialerSetDoNotDisturbCallback SetDoNotDisturbCallback { get; set; }
		public SimplDialerSetPrivacyMuteCallback SetPrivacyMuteCallback { get; set; }

		#endregion

		protected override void DisposeFinal(bool disposing)
		{
			OnSourceAdded = null;
			OnSourceRemoved = null;
			OnAutoAnswerChanged = null;
			OnDoNotDisturbChanged = null;
			OnPrivacyMuteChanged = null;

			DialCallback = null;
			DialTypeCallback = null;
			SetAutoAnswerCallback = null;
			SetDoNotDisturbCallback = null;
			SetPrivacyMuteCallback = null;

			base.DisposeFinal(disposing);

			SetShimSource(null);
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

		public void AddShimSource(IConferenceSource source)
		{
			if (m_Source == null)
				SetShimSource(source);
		}

		public void RemoveShimSource(IConferenceSource source)
		{
			if (m_Source == source)
				SetShimSource(null);
		}

		private void SetShimSource(IConferenceSource source)
		{
			if (source == m_Source)
				return;

			IConferenceSource oldSource = m_Source;

			m_Source = source;

			if (oldSource != null)
				OnSourceRemoved.Raise(this, new ConferenceSourceEventArgs(oldSource));

			if (m_Source != null)
				OnSourceAdded.Raise(this, new ConferenceSourceEventArgs(m_Source));
		}

		public IEnumerable<IConferenceSource> GetSources()
		{
			if (m_Source != null)
				yield return m_Source;
		}

		public bool ContainsSource(IConferenceSource source)
		{
			return source == m_Source;
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

			addRow("BoothId", BoothId);
			addRow("Language", Language);
			addRow("AutoAnswer", AutoAnswer);
			addRow("DND", DoNotDisturb);
			addRow("PrivacyMute", PrivacyMute);
		}

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;
			var source = m_Source;
			if (source != null)
				yield return source;
		}

		private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
		{
			return base.GetConsoleNodes();
		}

		#endregion
	}
}
