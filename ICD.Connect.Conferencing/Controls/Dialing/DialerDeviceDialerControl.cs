using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.Devices;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.IncomingCalls;

namespace ICD.Connect.Conferencing.Controls.Dialing
{
	public sealed class DialerDeviceDialerControl : AbstractConferenceDeviceControl<IDialerDevice, IConference>
	{
		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallAdded;
		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallRemoved;
		public override event EventHandler<ConferenceEventArgs> OnConferenceAdded;
		public override event EventHandler<ConferenceEventArgs> OnConferenceRemoved;

		private readonly IcdHashSet<IConference> m_Conferences;
		private readonly SafeCriticalSection m_ConferencesSection;

		public override eCallType Supports { get { return eCallType.Video; } }

		

		public DialerDeviceDialerControl(IDialerDevice parent, int id)
			: base(parent, id)
		{
			m_Conferences = new IcdHashSet<IConference>();
			m_ConferencesSection = new SafeCriticalSection();

			SupportedConferenceControlFeatures =
				eConferenceControlFeatures.AutoAnswer |
				eConferenceControlFeatures.DoNotDisturb |
				eConferenceControlFeatures.PrivacyMute |
				eConferenceControlFeatures.CanDial |
				eConferenceControlFeatures.CanEnd;
		}

		#region Parent Callbacks

		protected override void Subscribe(IDialerDevice parent)
		{
			base.Subscribe(parent);

			parent.OnConferenceAdded += ParentOnConferenceAdded;
			parent.OnConferenceRemoved += ParentOnConferenceRemoved;
			parent.OnIncomingCallAdded += ParentOnIncomingCallAdded;
			parent.OnIncomingCallRemoved += ParentOnIncomingCallRemoved;
			parent.OnPrivacyMuteChanged += ParentOnPrivacyMuteChanged;
			parent.OnAutoAnswerChanged += ParentOnAutoAnswerChanged;
			parent.OnDoNotDisturbChanged += ParentOnDoNotDisturbChanged;
		}

		protected override void Unsubscribe(IDialerDevice parent)
		{
			base.Unsubscribe(parent);

			parent.OnConferenceAdded -= ParentOnConferenceAdded;
			parent.OnConferenceRemoved -= ParentOnConferenceRemoved;
			parent.OnIncomingCallAdded -= ParentOnIncomingCallAdded;
			parent.OnIncomingCallRemoved -= ParentOnIncomingCallRemoved;
			parent.OnPrivacyMuteChanged -= ParentOnPrivacyMuteChanged;
			parent.OnAutoAnswerChanged -= ParentOnAutoAnswerChanged;
			parent.OnDoNotDisturbChanged -= ParentOnDoNotDisturbChanged;
		}

		private void ParentOnConferenceAdded(object sender, ConferenceEventArgs args)
		{
			AddConference(args.Data);
		}

		private void ParentOnConferenceRemoved(object sender, ConferenceEventArgs args)
		{
			RemoveConference(args.Data);
		}

		private void ParentOnIncomingCallAdded(object sender, GenericEventArgs<IIncomingCall> args)
		{
			OnIncomingCallAdded.Raise(this, new GenericEventArgs<IIncomingCall>(args.Data));
		}

		private void ParentOnIncomingCallRemoved(object sender, GenericEventArgs<IIncomingCall> args)
		{
			OnIncomingCallRemoved.Raise(this, new GenericEventArgs<IIncomingCall>(args.Data));
		}

		private void ParentOnPrivacyMuteChanged(object sender, BoolEventArgs eventArgs)
		{
			PrivacyMuted = Parent.PrivacyMuted;
		}

		private void ParentOnAutoAnswerChanged(object sender, BoolEventArgs eventArgs)
		{
			AutoAnswer = Parent.AutoAnswer;
		}

		private void ParentOnDoNotDisturbChanged(object sender, BoolEventArgs eventArgs)
		{
			DoNotDisturb = Parent.DoNotDisturb;
		}

		#endregion

		private void AddConference(IConference conference)
		{
			bool added;

			m_ConferencesSection.Enter();
			try
			{
				added = m_Conferences.Add(conference);
			}
			finally
			{
				m_ConferencesSection.Leave();
			}

			if (added)
				OnConferenceAdded.Raise(this, conference);
		}

		private void RemoveConference(IConference conference)
		{
			bool removed;
			
			m_ConferencesSection.Enter();
			try
			{
				removed = m_Conferences.Remove(conference);
			}
			finally
			{
				m_ConferencesSection.Leave();
			}

			if (removed)
				OnConferenceRemoved.Raise(this, conference);
		}

		public override IEnumerable<IConference> GetConferences()
		{
			return m_ConferencesSection.Execute(() => m_Conferences.ToArray(m_Conferences.Count));
		}

		public override eDialContextSupport CanDial(IDialContext dialContext)
		{
			return Parent.CanDial(dialContext);
		}

		public override void Dial(IDialContext dialContext)
		{
			Parent.Dial(dialContext);
		}

		public override void SetDoNotDisturb(bool enabled)
		{
			Parent.SetDoNotDisturb(enabled);
		}

		public override void SetAutoAnswer(bool enabled)
		{
			Parent.SetAutoAnswer(enabled);
		}

		public override void SetPrivacyMute(bool enabled)
		{
			Parent.SetPrivacyMute(enabled);
		}

		public override void SetCameraMute(bool mute)
		{
			throw new NotSupportedException();
		}

		public override void StartPersonalMeeting()
		{
			throw new NotSupportedException();
		}

		public override void EnableCallLock(bool enabled)
		{
			throw new NotSupportedException();
		}
	}
}
