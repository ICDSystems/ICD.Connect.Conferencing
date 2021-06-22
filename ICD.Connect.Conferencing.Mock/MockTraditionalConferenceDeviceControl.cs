using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.IncomingCalls;
using ICD.Connect.Conferencing.Participants.Enums;
using ICD.Common.Utils.Extensions;

namespace ICD.Connect.Conferencing.Mock
{
	public sealed class MockTraditionalConferenceDeviceControl : AbstractConferenceDeviceControl<IMockConferencingDevice, ThinConference>
	{
		#region Events

		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallAdded;
		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallRemoved;
		public override event EventHandler<ConferenceEventArgs> OnConferenceAdded;
		public override event EventHandler<ConferenceEventArgs> OnConferenceRemoved;

		#endregion

		#region Fields

		private readonly IcdHashSet<ThinConference> m_Conferences;
		private readonly SafeCriticalSection m_ConferencesSection;

		#endregion

		#region Properties

		public override eCallType Supports { get { return eCallType.Video | eCallType.Audio; } }

		#endregion

		public MockTraditionalConferenceDeviceControl(IMockConferencingDevice parent, int id)
			: base(parent, id)
		{
			m_Conferences = new IcdHashSet<ThinConference>();
			m_ConferencesSection = new SafeCriticalSection();


			SupportedConferenceControlFeatures =
				eConferenceControlFeatures.AutoAnswer |
				eConferenceControlFeatures.DoNotDisturb |
				eConferenceControlFeatures.PrivacyMute |
				eConferenceControlFeatures.CameraMute |
				eConferenceControlFeatures.Hold |
				eConferenceControlFeatures.Dtmf |
				eConferenceControlFeatures.CanDial |
				eConferenceControlFeatures.CanEnd;
		}

		public override IEnumerable<ThinConference> GetConferences()
		{
			return m_ConferencesSection.Execute(() => m_Conferences.ToArray(m_Conferences.Count));
		}

		private void AddConference([NotNull] ThinConference conference)
		{
			if (conference == null)
				throw new ArgumentNullException("conference");

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

		private void RemoveConference([NotNull] ThinConference conference)
		{
			if (conference == null)
				throw new ArgumentNullException("conference");

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

		public override eDialContextSupport CanDial(IDialContext dialContext)
		{
			return eDialContextSupport.Supported;
		}

		public override void Dial(IDialContext dialContext)
		{
			Dial(dialContext.DialString, dialContext.CallType);
		}

		public override void SetDoNotDisturb(bool enabled)
		{
			DoNotDisturb = enabled;
		}

		public override void SetAutoAnswer(bool enabled)
		{
			AutoAnswer = enabled;
		}

		public override void SetPrivacyMute(bool enabled)
		{
			PrivacyMuted = enabled;
		}

		public override void SetCameraMute(bool mute)
		{
			CameraMute = mute;
		}

		public override void StartPersonalMeeting()
		{
			var context = new DialContext
			{
				CallType = Supports,
				DialString = "Personal Meeting"
			};

			Dial(context);
		}

		public override void EnableCallLock(bool enabled)
		{
			throw new NotSupportedException("Call Lock is Not Supported");
		}


		private void Dial(string number, eCallType type)
		{
			ThinConference conference =
				new ThinConference
				{
					EndConferenceCallback = HangupCallback,
					DialTime = IcdEnvironment.GetUtcTime(),
					Direction = eCallDirection.Outgoing,
					Number = number,
					Name = "Mock Call To: " + number,
					Status = eConferenceStatus.Connected,
					CallType = type
				};

			AddConference(conference);
		}

		private void HangupCallback(ThinConference sender)
		{
			sender.Status = eConferenceStatus.Disconnected;
			sender.EndTime = IcdEnvironment.GetUtcTime();

			RemoveConference(sender);

			sender.Dispose();
		}

		private void MockIncomingCall()
		{
			// ReSharper disable once PossibleNullReferenceException
			var incomingCall = new TraditionalIncomingCall(Supports)
			{
				Name = "Mock Incoming Call",
				Number = "867-6309",
				AnswerCallback = AnswerCallback,
				RejectCallback = RejectCallback,
			};
			OnIncomingCallAdded.Raise(this,incomingCall);

			// Handle DND
			if (DoNotDisturb)
			{
				incomingCall.Reject();
			}

			// Handle Auto-Answer
			else if (AutoAnswer)
			{
				incomingCall.AnswerState = eCallAnswerState.AutoAnswered;
				ThinConference conference = ThinConference.FromIncomingCall(incomingCall);
				conference.EndConferenceCallback = HangupCallback;
				conference.Status = eConferenceStatus.Connected;
				OnIncomingCallRemoved.Raise(this, incomingCall);
				AddConference(conference);
			}
		}

		private void RejectCallback(IIncomingCall sender)
		{
			sender.AnswerState = eCallAnswerState.Rejected;
			OnIncomingCallRemoved.Raise(this, sender);
		}

		private void AnswerCallback(IIncomingCall sender)
		{
			sender.AnswerState = eCallAnswerState.Answered;
			ThinConference conference = ThinConference.FromIncomingCall(sender);
			conference.EndConferenceCallback = HangupCallback;
			conference.Status = eConferenceStatus.Connected;
			OnIncomingCallRemoved.Raise(this, sender);
			AddConference(conference);
		}

		#region Console

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			//yield return ConsoleNodeGroup.IndexNodeMap("Conferences", "", GetConferences());
		}

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
		{
			return base.GetConsoleNodes();
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new ConsoleCommand("MockIncomingCall", "Generates a mock incoming call", () => MockIncomingCall());
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
