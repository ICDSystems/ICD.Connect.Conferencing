using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Logging.LoggingContexts;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Timers;
using ICD.Common.Utils.Xml;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Dialing
{
	// Ignore missing comment warnings
#pragma warning disable 1591
	public enum eCiscoDialProtocol
	{
		[UsedImplicitly] Unknown,
		[UsedImplicitly] H320,
		[UsedImplicitly] H323,
		[UsedImplicitly] Sip,
		[UsedImplicitly] Spark
	}
#pragma warning restore 1591

	/// <summary>
	/// The dialing module provides Call functionality for the Cisco codec.
	/// </summary>
	public sealed class DialingComponent : AbstractCiscoComponent
	{
		private const int DONOTDISTURB_TIMEOUT_MIN = 1;
		private const int DONOTDISTURB_TIMEOUT_MAX = 1440;
		private const int ORPHANED_CALL_CHECK_RATE = 60 * 1000; //Every 60 seconds

		/// <summary>
		/// Called when a source is added to the dialing component.
		/// </summary>
		public event EventHandler<GenericEventArgs<CallStatus>> OnSourceAdded;

		/// <summary>
		/// Called when a source's state is changed.
		/// </summary>
		public event EventHandler<GenericEventArgs<CallStatus>> OnSourceUpdated;

		/// <summary>
		/// Called when a source is removed from the dialing component.
		/// </summary>
		public event EventHandler<GenericEventArgs<CallStatus>> OnSourceRemoved;

		/// <summary>
		/// Raised when the Do Not Disturb state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnDoNotDisturbChanged;

		/// <summary>
		/// Raised when the Auto Answer state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnAutoAnswerChanged;

		/// <summary>
		/// Raised when the microphones mute state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnPrivacyMuteChanged;

		private readonly Dictionary<int, CallStatus> m_Calls;
		private readonly SafeCriticalSection m_CallsSection;

		/// <summary>
		/// Timer to check all calls for orphaned calls
		/// ie calls that are no longer on the codec
		/// </summary>
		private readonly SafeTimer m_OrphanedCallTimer;

		private bool m_DoNotDisturb;
		private bool m_AutoAnswer;
		private bool m_PrivacyMuted;

		#region Properties

		/// <summary>
		/// Gets the DoNotDisturb state.
		/// </summary>
		public bool DoNotDisturb
		{
			get { return m_DoNotDisturb; }
			private set
			{
				if (value == m_DoNotDisturb)
					return;

				m_DoNotDisturb = value;

				Codec.Logger.LogSetTo(eSeverity.Informational, "DoNotDisturb", m_DoNotDisturb);

				OnDoNotDisturbChanged.Raise(this, new BoolEventArgs(m_DoNotDisturb));
			}
		}

		/// <summary>
		/// Gets the AutoAnswer state.
		/// </summary>
		public bool AutoAnswer
		{
			get { return m_AutoAnswer; }
			private set
			{
				if (value == m_AutoAnswer)
					return;

				m_AutoAnswer = value;

				Codec.Logger.LogSetTo(eSeverity.Informational, "AutoAnswer", m_AutoAnswer);

				OnAutoAnswerChanged.Raise(this, new BoolEventArgs(m_AutoAnswer));
			}
		}

		/// <summary>
		/// Get and the current microphone mute state.
		/// </summary>
		public bool PrivacyMuted
		{
			get { return m_PrivacyMuted; }
			private set
			{
				if (value == m_PrivacyMuted)
					return;

				m_PrivacyMuted = value;

				Codec.Logger.LogSetTo(eSeverity.Informational, "PrivacyMuted", m_PrivacyMuted);

				OnPrivacyMuteChanged.Raise(this, new BoolEventArgs(m_PrivacyMuted));
			}
		}

		/// <summary>
		/// Gets the max number of calls that can be active at a given time.
		/// </summary>
		public uint MaxActiveCalls { get; private set; }

		/// <summary>
		/// Gets the max number of audio calls that can be active at a given time.
		/// </summary>
		public uint MaxAudioCalls { get; private set; }

		/// <summary>
		/// Gets the max number of calls that can be online at a given time.
		/// </summary>
		public uint MaxCalls { get; private set; }

		/// <summary>
		/// Gets the max number of video calls that can be active at a given time.
		/// </summary>
		public uint MaxVideoCalls { get; private set; }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="codec"></param>
		public DialingComponent(CiscoCodecDevice codec) 
			: base(codec)
		{
			m_Calls = new Dictionary<int, CallStatus>();
			m_CallsSection = new SafeCriticalSection();
			m_OrphanedCallTimer = SafeTimer.Stopped(OrphanedTimerCallback);

			Subscribe(Codec);

			if (Codec.Initialized)
				Initialize();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Dials the given number.
		/// </summary>
		/// <param name="number"></param>
		public void Dial(string number)
		{
			Dial(number, eCallType.Video);
		}

		/// <summary>
		/// Dials the given number.
		/// </summary>
		/// <param name="number"></param>
		/// <param name="callType"></param>
		public void Dial(string number, eCallType callType)
		{
			if (callType == eCallType.Unknown)
				throw new ArgumentOutOfRangeException("callType", "Cannot dial call with callType: Unknown");

			Dial(number, eCiscoDialProtocol.Sip, callType);
		}

		/// <summary>
		/// Dial the given number.
		/// 
		/// If we are already in a call:
		///		Hold the existing call
		///		Dial the new call
		/// </summary>
		/// <param name="number"></param>
		/// <param name="protocol"></param>
		/// <param name="callType"></param>
		[PublicAPI]
		public void Dial(string number, eCiscoDialProtocol protocol, eCallType callType)
		{
			if (callType == eCallType.Unknown)
				throw new ArgumentOutOfRangeException("callType", "Cannot dial call with callType: Unknown");

			CallStatus existing = GetCalls().Where(c => c.Status.GetIsOnline()).FirstOrDefault();

			if (existing != null)
				Hold(existing);

			Codec.SendCommand("xCommand Dial Number: {0} Protocol: {1} CallType: {2}", number, protocol, EnumUtils.GetFlags(callType).Max());
			Codec.Logger.Log(eSeverity.Debug, "Dialing {0} Protocol: {1} CallType: {2}", number, protocol, callType);
		}

		/// <summary>
		/// Enables DoNotDisturb.
		/// </summary>
		/// <param name="enabled"></param>
		public void SetDoNotDisturb(bool enabled)
		{
			EnableDoNotDisturb(DONOTDISTURB_TIMEOUT_MAX, enabled);
		}

		/// <summary>
		/// Enables DoNotDisturb.
		/// </summary>
		/// <param name="minutesTimeout"></param>
		/// <param name="state"></param>
		[PublicAPI]
		public void EnableDoNotDisturb(int minutesTimeout, bool state)
		{
			minutesTimeout = ValidateDoNotDisturbTimeout(minutesTimeout);

			if (state)
				Codec.SendCommand("xCommand Conference DoNotDisturb Activate Timeout: {0}", minutesTimeout);
			else
				Codec.SendCommand("xCommand Conference DoNotDisturb Deactivate");

			Codec.Logger.Log(eSeverity.Informational, "Setting DND to {0}", state ? "On" : "Off");
		}

		/// <summary>
		/// Enables AutoAnswer.
		/// </summary>
		/// <param name="enabled"></param>
		public void SetAutoAnswer(bool enabled)
		{
			string value = enabled ? "On" : "Off";

			Codec.SendCommand("xConfiguration Conference AutoAnswer Mode: {0}", value);
			Codec.Logger.Log(eSeverity.Informational, "Setting Auto Answer {0}", value);
		}

		/// <summary>
		/// Enables privacy mute.
		/// </summary>
		/// <param name="enabled"></param>
		public void SetPrivacyMute(bool enabled)
		{
			string value = enabled ? "Mute" : "Unmute";

			Codec.SendCommand("xCommand Audio Microphones {0}", value);
			Codec.Logger.Log(eSeverity.Informational, "Setting VTC Mic Mute {0}", enabled ? "On" : "Off");
		}

		/// <summary>
		/// Answers the incoming call.
		/// </summary>
		/// <param name="call"></param>
		public void Answer([NotNull] CallStatus call)
		{
			if (call == null)
				throw new ArgumentNullException("call");

			Answer(call.CallId);
		}

		/// <summary>
		/// Answers the incoming call.
		/// </summary>
		/// <param name="callId"></param>
		public void Answer(int callId)
		{
			Codec.SendCommand("xCommand Call Accept CallId: {0}", callId);
			Codec.Logger.Log(eSeverity.Debug, "Answering Incoming Call {0}", callId);
		}

		/// <summary>
		/// Rejects the incoming call.
		/// </summary>
		/// <param name="call"></param>
		public void Reject([NotNull] CallStatus call)
		{
			if (call == null)
				throw new ArgumentNullException("call");

			Reject(call.CallId);
		}

		/// <summary>
		/// Rejects the incoming call.
		/// </summary>
		/// <param name="callId"></param>
		public void Reject(int callId)
		{
			Codec.SendCommand("xCommand Call Reject CallId: {0}", callId);
			Codec.Logger.Log(eSeverity.Debug, "Rejecting Incoming Call {0}", callId);
		}

		/// <summary>
		/// Holds the given call.
		/// </summary>
		/// <param name="call"></param>
		public void Hold([NotNull] CallStatus call)
		{
			if (call == null)
				throw new ArgumentNullException("call");

			if (call.Status == eCiscoCallStatus.OnHold)
				return;

			Hold(call.CallId);
		}

		/// <summary>
		/// Hods the given call.
		/// </summary>
		/// <param name="callId"></param>
		public void Hold(int callId)
		{
			Codec.SendCommand("xCommand Call Hold CallId: {0}", callId);
			Codec.Logger.Log(eSeverity.Debug, "Placing Call {0} on hold", callId);
		}       

		/// <summary>
		/// Resumes the call.
		/// </summary>
		/// <param name="call"></param>
		public void Resume([NotNull] CallStatus call)
		{
			if (call == null)
				throw new ArgumentNullException("call");

			if (call.Status != eCiscoCallStatus.OnHold)
				return;

			Resume(call.CallId);
		}

		public void Resume(int callId)
		{
			Codec.SendCommand("xCommand Call Resume CallId: {0}", callId);
			Codec.Logger.Log(eSeverity.Debug, "Resuming Call {0}", callId);
		}

		/// <summary>
		/// Disconnects call.
		/// </summary>
		/// <param name="call"></param>
		public void Hangup([NotNull] CallStatus call)
		{
			if (call == null)
				throw new ArgumentNullException("call");
			
			Hangup(call.CallId);
		}

		/// <summary>
		/// Disconnects call
		/// </summary>
		/// <param name="callId"></param>
		public void Hangup(int callId)
		{
			Codec.SendCommand("xCommand Call Disconnect CallId: {0}", callId);
			Codec.Logger.Log(eSeverity.Debug, "Disconnecting Call {0}", callId);
		}

		/// <summary>
		/// Combines this call and the call with the given id.
		/// </summary>
		/// <param name="call"></param>
		/// <param name="other"></param>
		public void Join([NotNull] CallStatus call, [NotNull] CallStatus other)
		{
			if (call == null)
				throw new ArgumentNullException("call");

			if (other == null)
				throw new ArgumentNullException("other");

			Codec.SendCommand("xCommand Call Join CallId: {0} CallId: {1}", call.CallId, other.CallId);
			Codec.Logger.Log(eSeverity.Debug, "Joining Call {0} with {1}", call.CallId, other.CallId);
		}

		/// <summary>
		/// Allows sending DTMF codes
		/// </summary>
		/// <param name="call"></param>
		/// <param name="data"></param>
		public void SendDtmf([NotNull] CallStatus call, string data)
		{
			if (call == null)
				throw new ArgumentNullException("call");

			SendDtmf(call.CallId, data);
		}

		/// <summary>
		/// Allows sending DTMF codes
		/// </summary>
		/// <param name="callId"></param>
		/// <param name="data"></param>
		public void SendDtmf(int callId, string data)
		{
			Codec.SendCommand("xCommand Call DTMFSend CallId: {0} DTMFString: \"{1}\"", callId, data);
			Codec.Logger.Log(eSeverity.Debug, "Sending DTMF tone {0} to call {1}", data, callId);
		}

		public void AuthenticateHostPin(CallStatus call, string pin)
		{
			AuthenticateHostPin(call.CallId, pin);
		}
		
		public void AuthenticateHostPin(int callId, string pin)
		{
			AuthenticatePin(callId, "Host", pin);
		}

		public void AuthenticatePanelistPin(CallStatus call, string pin)
		{
			AuthenticatePanelistPin(call.CallId, pin);
		}

		public void AuthenticatePanelistPin(int callId, string pin)
		{
			AuthenticatePin(callId, "Panelist", pin);
		}

		public void AuthenticateGuestPin(CallStatus call, string pin)
		{
			AuthenticateGuestPin(call.CallId, pin);
		}
		
		public void AuthenticateGuestPin(int callId, string pin)
		{
			AuthenticatePin(callId, "Guest", pin);
		}

		public void AuthenticateGuest(CallStatus call)
		{
			AuthenticateGuest(call.CallId);
		}

		public void AuthenticateGuest(int callId)
		{
			Codec.SendCommand("xCommand Conference Call AuthenticationResponse CallId: {0}, ParticipantRole: Guest",
				callId);
		}

		public void AuthenticateCoHostPin(CallStatus call, string pin)
		{
			AuthenticateCoHostPin(call.CallId, pin);
		}

		public void AuthenticateCoHostPin(int callId, string pin)
		{
			// ReSharper disable once StringLiteralTypo
			AuthenticatePin(callId, "Cohost", pin);
		}

		public void AuthenticatePresenterPin(CallStatus call, string pin)
		{
			AuthenticatePresenterPin(call.CallId, pin);
		}

		public void AuthenticatePresenterPin(int callId, string pin)
		{
			AuthenticatePin(callId, "Presenter", pin);
		}

		private void AuthenticatePin(int callId, string participantRole, string pin)
		{
			Codec.SendCommand(
				"xCommand Conference Call AuthenticationResponse CallId: {0}, ParticipantRole: {1}, Pin: {2}", callId, participantRole, pin);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Called to initialize the component.
		/// </summary>
		protected override void Initialize()
		{
			base.Initialize();
            
			m_OrphanedCallTimer.Reset(ORPHANED_CALL_CHECK_RATE, ORPHANED_CALL_CHECK_RATE);
		}

		/// <summary>
		/// Called to deinitialize the component.
		/// </summary>
		protected override void Deinitialize()
		{
			base.Deinitialize();

			m_OrphanedCallTimer.Stop();
		}

		/// <summary>
		/// Gets the child CallComponents in order of call id.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<CallStatus> GetCalls()
		{
			return m_CallsSection.Execute(() => m_Calls.OrderValuesByKey().ToArray());
		}

		/// <summary>
		/// Lazy Loads a call from XML, creates a new one if does not exist
		/// If the call already exists, updates the existing call
		/// If the call doesn't exist and is disconnected, it won't be added and null will be returned
		/// </summary>
		/// <param name="callId"></param>
		/// <param name="xml"></param>
		/// <returns></returns>
		[CanBeNull]
		private CallStatus LazyLoadCall(int callId, string xml)
		{
			CallStatus output;
			bool added = false;

			m_CallsSection.Enter();

			try
			{
				if (!m_Calls.TryGetValue(callId, out output))
				{
					output = CallStatus.FromXml(xml);

					if (output.Status == eCiscoCallStatus.Disconnected || output.Status == eCiscoCallStatus.Orphaned)
						return null;

					Subscribe(output);
					m_Calls[callId] = output;
					added = true;
				}
			}
			finally
			{
				m_CallsSection.Leave();
			}

			if (!added)
			{
				output.UpdateFromXml(xml);

				OnSourceUpdated.Raise(this, new GenericEventArgs<CallStatus>(output));

				return output;
			}

			// Join the new call to an existing, held call
			CallStatus other = GetCalls().FirstOrDefault(c => c.Status == eCiscoCallStatus.OnHold);

			if (other != null)
				Join(output, other);

			OnSourceAdded.Raise(this, new GenericEventArgs<CallStatus>(output));

			return output;
		}

		/// <summary>
		/// Removes the call and unsubscribes from the events.
		/// </summary>
		/// <param name="id"></param>
		/// <returns>True if the call was removed.</returns>
		private bool RemoveCall(int id)
		{
			m_CallsSection.Enter();

			try
			{
				CallStatus call;
				if (!m_Calls.Remove(id, out call))
					return false;

				Unsubscribe(call);
			}
			finally
			{
				m_CallsSection.Leave();
			}

			return true;
		}

		/// <summary>
		/// Returns a valid DoNotDisturb timeout value.
		/// </summary>
		/// <param name="timeoutMinutes"></param>
		/// <returns></returns>
		private static int ValidateDoNotDisturbTimeout(int timeoutMinutes)
		{
			return MathUtils.Clamp(timeoutMinutes, DONOTDISTURB_TIMEOUT_MIN, DONOTDISTURB_TIMEOUT_MAX);
		}

		private void OrphanedTimerCallback()
		{
			Codec.SendCommand("xStatus Call", OrphanedCallStatusCallback);
		}

		private void OrphanedCallStatusCallback(CiscoCodecDevice codec, string resultId, string xml)
        {
            IcdHashSet<int> callIds = new IcdHashSet<int>();

            if (!StringUtils.IsNullOrWhitespace(xml))
            {
                foreach (string callXml in XmlUtils.ReadListFromXml(xml,  "Call", c => c))
                {
                    int id = XmlUtils.GetAttributeAsInt(callXml, "item");
                    callIds.Add(id);
                    LazyLoadCall(id, callXml);
                }
            }

            CallStatus[] orphanedCalls;
            m_CallsSection.Enter();
            try
            {
                orphanedCalls = m_Calls.Values.Where(c => !callIds.Contains(c.CallId)).ToArray();
            }
            finally
            {
				m_CallsSection.Leave();
            }
            
			orphanedCalls.ForEach(c => c.SetOrphaned());
        }

		#endregion

		#region Call Status Callbacks

		private void Subscribe(CallStatus callStatus)
		{
			callStatus.OnStatusChanged += CallStatusOnStatusChanged;
		}


		private void Unsubscribe(CallStatus callStatus)
		{
			callStatus.OnStatusChanged -= CallStatusOnStatusChanged;
		}

		private void CallStatusOnStatusChanged(object sender, GenericEventArgs<eCiscoCallStatus> args)
		{
			CallStatus call = sender as CallStatus;
			if (call == null)
				return;

			if (args.Data != eCiscoCallStatus.Disconnected && args.Data != eCiscoCallStatus.Orphaned)
				return;

			if (args.Data == eCiscoCallStatus.Orphaned)
				Codec.Logger.Log(eSeverity.Warning, "Removing orphaned callId:{0}", call.CallId);

			if (RemoveCall(call.CallId))
				OnSourceRemoved.Raise(this, new GenericEventArgs<CallStatus>(call));
		}

		#endregion

		#region Codec Callbacks

		/// <summary>
		/// Subscribes to the codec events.
		/// </summary>
		/// <param name="codec"></param>
		protected override void Subscribe(CiscoCodecDevice codec)
		{
			base.Subscribe(codec);

			if (codec == null)
				return;

			codec.RegisterParserCallback(ParseMaxActiveCalls, CiscoCodecDevice.XSTATUS_ELEMENT, "Capabilities", "Conference",
			                             "MaxActiveCalls");
			codec.RegisterParserCallback(ParseMaxAudioCalls, CiscoCodecDevice.XSTATUS_ELEMENT, "Capabilities", "Conference",
			                             "MaxAudioCalls");
			codec.RegisterParserCallback(ParseMaxCalls, CiscoCodecDevice.XSTATUS_ELEMENT, "Capabilities", "Conference",
			                             "MaxCalls");
			codec.RegisterParserCallback(ParseMaxVideoCalls, CiscoCodecDevice.XSTATUS_ELEMENT, "Capabilities", "Conference",
			                             "MaxVideoCalls");

			codec.RegisterParserCallback(ParseCallStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "Call");
			codec.RegisterParserCallback(ParseDoNotDisturbStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "Conference", "DoNotDisturb");
			codec.RegisterParserCallback(ParseAutoAnswerMode, CiscoCodecDevice.XCONFIGURATION_ELEMENT, "Conference", "AutoAnswer",
			                             "Mode");
			codec.RegisterParserCallback(ParseMuteStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "Audio", "Microphones", "Mute");
		}

		/// <summary>
		/// Unsubscribes from the codec events.
		/// </summary>
		/// <param name="codec"></param>
		protected override void Unsubscribe(CiscoCodecDevice codec)
		{
			base.Unsubscribe(codec);

			if (codec == null)
				return;

			codec.UnregisterParserCallback(ParseMaxActiveCalls, CiscoCodecDevice.XSTATUS_ELEMENT, "Capabilities", "Conference",
			                               "MaxActiveCalls");
			codec.UnregisterParserCallback(ParseMaxAudioCalls, CiscoCodecDevice.XSTATUS_ELEMENT, "Capabilities", "Conference",
			                               "MaxAudioCalls");
			codec.UnregisterParserCallback(ParseMaxCalls, CiscoCodecDevice.XSTATUS_ELEMENT, "Capabilities", "Conference",
			                               "MaxCalls");
			codec.UnregisterParserCallback(ParseMaxVideoCalls, CiscoCodecDevice.XSTATUS_ELEMENT, "Capabilities", "Conference",
			                               "MaxVideoCalls");

			codec.UnregisterParserCallback(ParseCallStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "Call");
			codec.UnregisterParserCallback(ParseDoNotDisturbStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "Conference", "DoNotDisturb");
			codec.UnregisterParserCallback(ParseAutoAnswerMode, CiscoCodecDevice.XCONFIGURATION_ELEMENT, "Conference", "AutoAnswer",
			                               "Mode");
			codec.UnregisterParserCallback(ParseMuteStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "Audio", "Microphones", "Mute");
		}

		private void ParseMaxActiveCalls(CiscoCodecDevice codec, string resultid, string xml)
		{
			string content = XmlUtils.GetInnerXml(xml);
			MaxActiveCalls = uint.Parse(content);
		}

		private void ParseMaxAudioCalls(CiscoCodecDevice codec, string resultid, string xml)
		{
			string content = XmlUtils.GetInnerXml(xml);
			MaxAudioCalls = uint.Parse(content);
		}

		private void ParseMaxCalls(CiscoCodecDevice codec, string resultid, string xml)
		{
			string content = XmlUtils.GetInnerXml(xml);
			MaxCalls = uint.Parse(content);
		}

		private void ParseMaxVideoCalls(CiscoCodecDevice codec, string resultid, string xml)
		{
			string content = XmlUtils.GetInnerXml(xml);
			MaxVideoCalls = uint.Parse(content);
		}

		private void ParseMuteStatus(CiscoCodecDevice sender, string resultId, string xml)
		{
			PrivacyMuted = XmlUtils.GetInnerXml(xml) == "On";
		}

		private void ParseAutoAnswerMode(CiscoCodecDevice sender, string resultId, string xml)
		{
			AutoAnswer = XmlUtils.GetInnerXml(xml) == "On";
		}

		private void ParseDoNotDisturbStatus(CiscoCodecDevice sender, string resultId, string xml)
		{
			DoNotDisturb = XmlUtils.GetInnerXml(xml) == "Active";
		}

		/// <summary>
		/// Parses call statuses.
		/// </summary>
		private void ParseCallStatus(CiscoCodecDevice sender, string resultId, string xml)
		{
			int id = XmlUtils.GetAttributeAsInt(xml, "item");
			LazyLoadCall(id, xml);
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<string>("Dial", "Dial x", s => Dial(s));
			yield return new ConsoleCommand("ToggleAutoAnswer", "Toggles the auto-answer state",
			                                () => SetAutoAnswer(!AutoAnswer));
			yield return new ConsoleCommand("ToggleDoNotDisturb", "Toggles the do-not-disturb state",
			                                () => SetDoNotDisturb(!DoNotDisturb));
			yield return new ConsoleCommand("TogglePrivacyMute", "Toggles the privacy mute state",
			                                () => SetPrivacyMute(!PrivacyMuted));
			yield return new GenericConsoleCommand<int>("Hold", "Holds the specified call ID", id => Hold(id));
			yield return new GenericConsoleCommand<int>("Resume", "Resumes the call", id => Resume(id));
			yield return new GenericConsoleCommand<int>("Hangup", "Ends the call", id => Hangup(id));
			yield return new GenericConsoleCommand<int>("Answer", "Answers the incoming call", id => Answer(id));
			yield return new GenericConsoleCommand<int>("Reject", "Rejects the incoming call", id => Reject(id));
			yield return new GenericConsoleCommand<int, string>("SendDTMF", "SendDTMF x", (id, data) => SendDtmf(id, data));
		}

		/// <summary>
		/// Shim method to avoid "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		/// <summary>
		/// Gets the child console node groups.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			CallStatus[] calls = m_CallsSection.Execute(() => m_Calls.Values.ToArray(m_Calls.Count));
			yield return ConsoleNodeGroup.KeyNodeMap("Calls", "The current registered calls", calls, c => (uint)c.CallId);
		}

		/// <summary>
		/// Shim method to avoid "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
		{
			return base.GetConsoleNodes();
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			m_CallsSection.Execute(() => addRow("Calls Count", m_Calls.Count));
			addRow("Do-Not-Disturb", DoNotDisturb);
			addRow("Auto-Answer", AutoAnswer);
			addRow("Privacy Muted", PrivacyMuted);
		}

		#endregion
	}
}
