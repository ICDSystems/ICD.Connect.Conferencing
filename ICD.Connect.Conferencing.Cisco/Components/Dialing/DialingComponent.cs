﻿using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Services.Logging;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Xml;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.Cisco.Components.Dialing
{
	// Ignore missing comment warnings
#pragma warning disable 1591
	public enum eDialProtocol
	{
		[UsedImplicitly] H320,
		[UsedImplicitly] H323,
		[UsedImplicitly] Sip
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
		public event EventHandler<ConferenceSourceEventArgs> OnSourceAdded;

		/// <summary>
		/// Called when a source is removed from the dialing component.
		/// </summary>
		public event EventHandler<ConferenceSourceEventArgs> OnSourceRemoved;

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

		private readonly Dictionary<int, CallComponent> m_CallComponents;
		private readonly SafeCriticalSection m_CallComponentsSection;

		private bool m_CachedDoNotDisturbState;
		private bool m_CachedAutoAnswerState;
		private bool m_CachedPrivacyMuteState;

		#region Properties

		/// <summary>
		/// Gets the DoNotDisturb state.
		/// </summary>
		public bool DoNotDisturb
		{
			get { return m_CachedDoNotDisturbState; }
			private set
			{
				if (value == m_CachedDoNotDisturbState)
					return;

				m_CachedDoNotDisturbState = value;

				Codec.Log(eSeverity.Informational, "DND is {0}", m_CachedDoNotDisturbState ? "On" : "Off");

				OnDoNotDisturbChanged.Raise(this, new BoolEventArgs(m_CachedDoNotDisturbState));
			}
		}

		/// <summary>
		/// Gets the AutoAnswer state.
		/// </summary>
		public bool AutoAnswer
		{
			get { return m_CachedAutoAnswerState; }
			private set
			{
				if (value == m_CachedAutoAnswerState)
					return;

				m_CachedAutoAnswerState = value;

				Codec.Log(eSeverity.Informational, "Auto Answer is {0}", m_CachedAutoAnswerState ? "On" : "Off");

				OnAutoAnswerChanged.Raise(this, new BoolEventArgs(m_CachedAutoAnswerState));
			}
		}

		/// <summary>
		/// Get and the current microphone mute state.
		/// </summary>
		public bool PrivacyMuted
		{
			get { return m_CachedPrivacyMuteState; }
			private set
			{
				if (value == m_CachedPrivacyMuteState)
					return;

				m_CachedPrivacyMuteState = value;

				Codec.Log(eSeverity.Informational, "VTC Microphone Mute is {0}", m_CachedPrivacyMuteState ? "On" : "Off");

				OnPrivacyMuteChanged.Raise(this, new BoolEventArgs(m_CachedPrivacyMuteState));
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
		public DialingComponent(CiscoCodec codec) : base(codec)
		{
			m_CallComponents = new Dictionary<int, CallComponent>();
			m_CallComponentsSection = new SafeCriticalSection();

			Subscribe(Codec);

			if (Codec.Initialized)
				Initialize();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Gets the active conference sources.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConferenceSource> GetSources()
		{
			return m_CallComponentsSection.Execute(() => m_CallComponents.OrderValuesByKey().ToArray());
		}

		/// <summary>
		/// Dials the given number.
		/// </summary>
		/// <param name="number"></param>
		public void Dial(string number)
		{
			Dial(number, eConferenceSourceType.Video);
		}

		/// <summary>
		/// Dials the given number.
		/// </summary>
		/// <param name="number"></param>
		/// <param name="callType"></param>
		public void Dial(string number, eConferenceSourceType callType)
		{
			Dial(number, eDialProtocol.Sip, callType);
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
		public void Dial(string number, eDialProtocol protocol, eConferenceSourceType callType)
		{
			CallComponent existing = GetCallComponents().Where(c => c.GetIsOnline()).FirstOrDefault();
			if (existing != null)
				existing.Hold();

			Codec.SendCommand("xCommand Dial Number: {0} Protocol: {1} CallType: {2}", number, protocol, callType);
			Codec.Log(eSeverity.Debug, "Dialing {0} Protocol: {1} CallType: {2}", number, protocol, callType);
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

			Codec.Log(eSeverity.Informational, "Setting DND to {0}", state ? "On" : "Off");
		}

		/// <summary>
		/// Enables AutoAnswer.
		/// </summary>
		/// <param name="enabled"></param>
		public void SetAutoAnswer(bool enabled)
		{
			string value = enabled ? "On" : "Off";

			Codec.SendCommand("xConfiguration Conference AutoAnswer Mode: {0}", value);
			Codec.Log(eSeverity.Informational, "Setting Auto Answer {0}", value);
		}

		/// <summary>
		/// Enables privacy mute.
		/// </summary>
		/// <param name="enabled"></param>
		public void SetPrivacyMute(bool enabled)
		{
			string value = enabled ? "Mute" : "Unmute";

			Codec.SendCommand("xCommand Audio Microphones {0}", value);
			Codec.Log(eSeverity.Informational, "Setting VTC Mic Mute {0}", enabled ? "On" : "Off");
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Instantiates a call if it doesn't exist already.
		/// </summary>
		/// <param name="callId"></param>
		/// <param name="xml"></param>
		private CallComponent LazyLoadCall(int callId, string xml)
		{
			CallComponent output;
			bool added = false;

			m_CallComponentsSection.Enter();

			try
			{
				if (m_CallComponents.ContainsKey(callId))
					output = m_CallComponents[callId];
				else
				{
					output = BuildCall(callId);
					m_CallComponents[callId] = output;
					added = true;
				}
			}
			finally
			{
				m_CallComponentsSection.Leave();
			}

			output.Parse(xml);

			if (!added)
				return output;

			// Join the new call to an existing, held call
			CallComponent other = GetCallComponents().Where(c => c.Status == eConferenceSourceStatus.OnHold)
			                                         .FirstOrDefault();
			if (other != null)
				output.Join(other.CallId);

			OnSourceAdded.Raise(this, new ConferenceSourceEventArgs(output));

			return output;
		}

		/// <summary>
		/// Gets the child CallComponents in order of call id.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<CallComponent> GetCallComponents()
		{
			return m_CallComponentsSection.Execute(() => m_CallComponents.OrderValuesByKey().ToArray());
		}

		/// <summary>
		/// Instantiates the call and subscribes to the events.
		/// </summary>
		/// <param name="callId"></param>
		/// <returns></returns>
		private CallComponent BuildCall(int callId)
		{
			CallComponent output = new CallComponent(callId, Codec);
			Subscribe(output);

			return output;
		}

		/// <summary>
		/// Removes the call and unsubscribes from the events.
		/// </summary>
		/// <param name="id"></param>
		/// <returns>True if the call was removed.</returns>
		private bool RemoveCall(int id)
		{
			m_CallComponentsSection.Enter();

			try
			{
				if (!m_CallComponents.ContainsKey(id))
					return false;

				CallComponent call = m_CallComponents[id];
				m_CallComponents.Remove(id);

				Unsubscribe(call);
			}
			finally
			{
				m_CallComponentsSection.Leave();
			}

			return true;
		}

		/// <summary>
		/// Subscribes to the call events.
		/// </summary>
		/// <param name="call"></param>
		private void Subscribe(IConferenceSource call)
		{
			call.OnStatusChanged += CallOnStatusChanged;
		}

		/// <summary>
		/// Unsubscribes from the call events.
		/// </summary>
		/// <param name="call"></param>
		private void Unsubscribe(IConferenceSource call)
		{
			call.OnStatusChanged -= CallOnStatusChanged;
		}

		/// <summary>
		/// Called when a call status changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void CallOnStatusChanged(object sender, ConferenceSourceStatusEventArgs args)
		{
			CallComponent call = sender as CallComponent;
			if (call == null)
				return;

			if (args.Data != eConferenceSourceStatus.Disconnected)
				return;

			bool removed = RemoveCall(call.CallId);
			if (removed)
				OnSourceRemoved.Raise(this, new ConferenceSourceEventArgs(call));
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

		#region Codec Callbacks

		/// <summary>
		/// Subscribes to the codec events.
		/// </summary>
		/// <param name="codec"></param>
		protected override void Subscribe(CiscoCodec codec)
		{
			base.Subscribe(codec);

			if (codec == null)
				return;

			codec.RegisterParserCallback(ParseMaxActiveCalls, CiscoCodec.XSTATUS_ELEMENT, "Capabilities", "Conference",
			                             "MaxActiveCalls");
			codec.RegisterParserCallback(ParseMaxAudioCalls, CiscoCodec.XSTATUS_ELEMENT, "Capabilities", "Conference",
			                             "MaxAudioCalls");
			codec.RegisterParserCallback(ParseMaxCalls, CiscoCodec.XSTATUS_ELEMENT, "Capabilities", "Conference",
			                             "MaxCalls");
			codec.RegisterParserCallback(ParseMaxVideoCalls, CiscoCodec.XSTATUS_ELEMENT, "Capabilities", "Conference",
			                             "MaxVideoCalls");

			codec.RegisterParserCallback(ParseCallStatus, CiscoCodec.XSTATUS_ELEMENT, "Call");
			codec.RegisterParserCallback(ParseDoNotDisturbStatus, CiscoCodec.XSTATUS_ELEMENT, "Conference", "DoNotDisturb");
			codec.RegisterParserCallback(ParseAutoAnswerMode, CiscoCodec.XCONFIGURATION_ELEMENT, "Conference", "AutoAnswer",
			                             "Mode");
			codec.RegisterParserCallback(ParseMuteStatus, CiscoCodec.XSTATUS_ELEMENT, "Audio", "Microphones", "Mute");
		}

		/// <summary>
		/// Unsubscribes from the codec events.
		/// </summary>
		/// <param name="codec"></param>
		protected override void Unsubscribe(CiscoCodec codec)
		{
			base.Unsubscribe(codec);

			if (codec == null)
				return;

			codec.UnregisterParserCallback(ParseMaxActiveCalls, CiscoCodec.XSTATUS_ELEMENT, "Capabilities", "Conference",
			                               "MaxActiveCalls");
			codec.UnregisterParserCallback(ParseMaxAudioCalls, CiscoCodec.XSTATUS_ELEMENT, "Capabilities", "Conference",
			                               "MaxAudioCalls");
			codec.UnregisterParserCallback(ParseMaxCalls, CiscoCodec.XSTATUS_ELEMENT, "Capabilities", "Conference",
			                               "MaxCalls");
			codec.UnregisterParserCallback(ParseMaxVideoCalls, CiscoCodec.XSTATUS_ELEMENT, "Capabilities", "Conference",
			                               "MaxVideoCalls");

			codec.UnregisterParserCallback(ParseCallStatus, CiscoCodec.XSTATUS_ELEMENT, "Call");
			codec.UnregisterParserCallback(ParseDoNotDisturbStatus, CiscoCodec.XSTATUS_ELEMENT, "Conference", "DoNotDisturb");
			codec.UnregisterParserCallback(ParseAutoAnswerMode, CiscoCodec.XCONFIGURATION_ELEMENT, "Conference", "AutoAnswer",
			                               "Mode");
			codec.UnregisterParserCallback(ParseMuteStatus, CiscoCodec.XSTATUS_ELEMENT, "Audio", "Microphones", "Mute");
		}

		private void ParseMaxActiveCalls(CiscoCodec codec, string resultid, string xml)
		{
			string content = XmlUtils.GetInnerXml(xml);
			MaxActiveCalls = uint.Parse(content);
		}

		private void ParseMaxAudioCalls(CiscoCodec codec, string resultid, string xml)
		{
			string content = XmlUtils.GetInnerXml(xml);
			MaxAudioCalls = uint.Parse(content);
		}

		private void ParseMaxCalls(CiscoCodec codec, string resultid, string xml)
		{
			string content = XmlUtils.GetInnerXml(xml);
			MaxCalls = uint.Parse(content);
		}

		private void ParseMaxVideoCalls(CiscoCodec codec, string resultid, string xml)
		{
			string content = XmlUtils.GetInnerXml(xml);
			MaxVideoCalls = uint.Parse(content);
		}

		private void ParseMuteStatus(CiscoCodec sender, string resultId, string xml)
		{
			PrivacyMuted = XmlUtils.GetInnerXml(xml) == "On";
		}

		private void ParseAutoAnswerMode(CiscoCodec sender, string resultId, string xml)
		{
			AutoAnswer = XmlUtils.GetInnerXml(xml) == "On";
		}

		private void ParseDoNotDisturbStatus(CiscoCodec sender, string resultId, string xml)
		{
			DoNotDisturb = XmlUtils.GetInnerXml(xml) == "Active";
		}

		/// <summary>
		/// Parses call statuses.
		/// </summary>
		private void ParseCallStatus(CiscoCodec sender, string resultId, string xml)
		{
			// Instantiate a CallState for the call id.
			int id = XmlUtils.GetAttributeAsInt(xml, "item");
			bool exists = m_CallComponentsSection.Execute(() => m_CallComponents.ContainsKey(id));

			CallComponent call = LazyLoadCall(id, xml);

			if (!exists && call.Direction == eConferenceSourceDirection.Incoming)
				Codec.Log(eSeverity.Informational, "Incoming VTC Call");
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

			CallComponent[] calls = m_CallComponentsSection.Execute(() => m_CallComponents.Values.ToArray());
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

			m_CallComponentsSection.Execute(() => addRow("Calls Count", m_CallComponents.Count));
			addRow("Do-Not-Disturb", DoNotDisturb);
			addRow("Auto-Answer", AutoAnswer);
			addRow("Privacy Muted", PrivacyMuted);
		}

		#endregion
	}
}
