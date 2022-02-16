﻿using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Logging.LoggingContexts;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Xml;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants.Enums;

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

			if (call.Status == eParticipantStatus.OnHold)
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

			if (call.Status != eParticipantStatus.OnHold)
				return;

			Resume(call.CallId);
		}

		public void Resume(int callId)
		{
			Codec.SendCommand("xCommand Call Resume CallId: {0}", callId);
			Codec.Logger.Log(eSeverity.Debug, "Resuming Call {0}", callId);
		}

		/// <summary>
		/// Disconnects all calls.
		/// </summary>
		/// <param name="call"></param>
		public void Hangup([NotNull] CallStatus call)
		{
			if (call == null)
				throw new ArgumentNullException("call");

			Codec.SendCommand("xCommand Call Disconnect CallId: {0}", call.CallId);
			Codec.Logger.Log(eSeverity.Debug, "Disconnecting Call {0}", call.CallId);
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
		/// Allows sending data to dial-tone menus.
		/// </summary>
		/// <param name="call"></param>
		/// <param name="data"></param>
		public void SendDtmf([NotNull] CallStatus call, string data)
		{
			if (call == null)
				throw new ArgumentNullException("call");

			Codec.SendCommand("xCommand Call DTMFSend CallId: {0} DTMFString: \"{1}\"", call.CallId, data);
			Codec.Logger.Log(eSeverity.Debug, "Sending DTMF tone {0} to call {1}", data, call.CallId);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Gets the child CallComponents in order of call id.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<CallStatus> GetCalls()
		{
			return m_CallsSection.Execute(() => m_Calls.OrderValuesByKey().ToArray());
		}

		private CallStatus LazyLoadCall(int callId, string xml)
		{
			CallStatus output;
			bool added = false;

			m_CallsSection.Enter();

			try
			{
				if (m_Calls.ContainsKey(callId))
					output = m_Calls[callId];
				else
				{
					output = CallStatus.FromXml(xml);
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
			CallStatus other = GetCalls().FirstOrDefault(c => c.Status == eParticipantStatus.OnHold);

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
				if (!m_Calls.ContainsKey(id))
					return false;

				CallStatus call = m_Calls[id];
				m_Calls.Remove(id);

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

		private void CallStatusOnStatusChanged(object sender, GenericEventArgs<eParticipantStatus> args)
		{
			CallStatus call = sender as CallStatus;
			if (call == null)
				return;

			if (args.Data != eParticipantStatus.Disconnected)
				return;

			bool removed = RemoveCall(call.CallId);
			if (removed)
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
			bool exists = m_CallsSection.Execute(() => m_Calls.ContainsKey(id));

			CallStatus call = LazyLoadCall(id, xml);

			if (!exists && call.Direction == eCallDirection.Incoming)
				Codec.Logger.Log(eSeverity.Informational, "Incoming VTC Call");
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
			yield return new GenericConsoleCommand<int>("Hold", "Holds the specified call ID", id => HoldHelper(id));
			yield return new GenericConsoleCommand<int>("Resume", "Resumes the call", id => ResumeHelper(id));
			yield return new GenericConsoleCommand<int>("Hangup", "Ends the call", id => HangupHelper(id));
			yield return new GenericConsoleCommand<int>("Answer", "Answers the incoming call", id => AnswerHelper(id));
			yield return new GenericConsoleCommand<int>("Reject", "Rejects the incoming call", id => RejectHelper(id));
			yield return new GenericConsoleCommand<int, string>("SendDTMF", "SendDTMF x", (id, data) => SendDtmfHelper(id, data));
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

			CallStatus[] calls = m_CallsSection.Execute(() => m_Calls.Values.ToArray());
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

		#region Helpers

		private void HoldHelper(int id)
		{
			m_CallsSection.Enter();
			try
			{
				if (!m_Calls.ContainsKey(id))
				{
					Codec.Logger.Log(eSeverity.Warning, "No call with specified ID {0} - cannot hold.", id);
					return;
				}

				Hold(m_Calls[id]);
			}
			finally
			{
				m_CallsSection.Leave();
			}
		}

		private void ResumeHelper(int id)
		{
			m_CallsSection.Enter();
			try
			{
				if (!m_Calls.ContainsKey(id))
				{
					Codec.Logger.Log(eSeverity.Warning, "No call with specified ID {0} - cannot resume.", id);
					return;
				}

				Resume(m_Calls[id]);
			}
			finally
			{
				m_CallsSection.Leave();
			}
		}

		private void HangupHelper(int id)
		{
			m_CallsSection.Enter();
			try
			{
				if (!m_Calls.ContainsKey(id))
				{
					Codec.Logger.Log(eSeverity.Warning, "No call with specified ID {0} - cannot hangup.", id);
					return;
				}

				Hangup(m_Calls[id]);
			}
			finally
			{
				m_CallsSection.Leave();
			}
		}

		private void AnswerHelper(int id)
		{
			m_CallsSection.Enter();
			try
			{
				if (!m_Calls.ContainsKey(id))
				{
					Codec.Logger.Log(eSeverity.Warning, "No call with specified ID {0} - cannot answer.", id);
					return;
				}

				Answer(m_Calls[id]);
			}
			finally
			{
				m_CallsSection.Leave();
			}
		}

		private void RejectHelper(int id)
		{
			m_CallsSection.Enter();
			try
			{
				if (!m_Calls.ContainsKey(id))
				{
					Codec.Logger.Log(eSeverity.Warning, "No call with specified ID {0} - cannot reject.", id);
					return;
				}

				Reject(m_Calls[id]);
			}
			finally
			{
				m_CallsSection.Leave();
			}
		}

		private void SendDtmfHelper(int id, string data)
		{
			m_CallsSection.Enter();
			try
			{
				if (!m_Calls.ContainsKey(id))
				{
					Codec.Logger.Log(eSeverity.Warning, "No call with specified ID {0} - cannot send DTMF.", id);
					return;
				}

				SendDtmf(m_Calls[id], data);
			}
			finally
			{
				m_CallsSection.Leave();
			}
		}

		#endregion

		#endregion
	}
}
