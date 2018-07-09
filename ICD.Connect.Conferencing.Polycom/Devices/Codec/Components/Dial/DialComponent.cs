using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Dial
{
	public sealed class DialComponent : AbstractPolycomComponent
	{
		/// <summary>
		/// Raised when a call state is added, removed or updated.
		/// </summary>
		public event EventHandler OnCallStatesChanged; 

		private static readonly BiDictionary<eDialProtocol, string> s_ProtocolNames =
			new BiDictionary<eDialProtocol, string>
			{
				{eDialProtocol.Sip, "sip"},
				{eDialProtocol.H323, "h323"},
				{eDialProtocol.Auto, "auto"},
				{eDialProtocol.SipSpeakerphone, "sip_speakerphone"}
			};

		private static readonly BiDictionary<eDialType, string> s_TypeNames =
			new BiDictionary<eDialType, string>
			{
				{eDialType.H323, "h323"},
				{eDialType.Ip, "ip"},
				{eDialType.Sip, "sip"},
				{eDialType.Gateway, "gateway"}
			};

		private readonly IcdOrderedDictionary<int, CallStatus> m_CallStates;
		private readonly SafeCriticalSection m_CallStatesSection;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="codec"></param>
		public DialComponent(PolycomGroupSeriesDevice codec)
			: base(codec)
		{
			m_CallStates = new IcdOrderedDictionary<int, CallStatus>();
			m_CallStatesSection = new SafeCriticalSection();

			Subscribe(Codec);

			Codec.RegisterFeedback("cs", HandleCallState);
			Codec.RegisterFeedback("active", HandleActiveCall);
			Codec.RegisterFeedback("cleared", HandleClearedCall);
			Codec.RegisterFeedback("ended", HandleEndedCall);
			Codec.RegisterFeedback("notification", HandleNotification);

			Codec.RegisterRangeFeedback("callinfo", HandleCallRangeInfo);

			if (Codec.Initialized)
				Initialize();
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			OnCallStatesChanged = null;

			base.Dispose(disposing);
		}

		/// <summary>
		/// Called to initialize the component.
		/// </summary>
		protected override void Initialize()
		{
			base.Initialize();

			Codec.EnqueueCommand("callstate register");
			Codec.EnqueueCommand("notify callstatus");
			Codec.EnqueueCommand("notify linestatus");
			Codec.EnqueueCommand("listen video");

			Codec.EnqueueCommand("callinfo all");
			Codec.EnqueueCommand("callstate get");
		}

		#region Methods

		/// <summary>
		/// Returns the active call statuses.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<CallStatus> GetCallStatuses()
		{
			return m_CallStatesSection.Execute(() => m_CallStates.Values.ToArray(m_CallStates.Count));
		}

		/// <summary>
		/// Answers the incoming video call.
		/// </summary>
		public void AnswerVideo()
		{
			Codec.EnqueueCommand("answer video");
			Codec.Log(eSeverity.Informational, "Answering incoming video call");
		}

		/// <summary>
		/// Disconnects the given video call.
		/// </summary>
		/// <param name="callId"></param>
		public void HangupVideo(int callId)
		{
			Codec.EnqueueCommand("hangup video {0}", callId);
			Codec.Log(eSeverity.Informational, "Disconnecting video call {0}", callId);
		}

		/// <summary>
		/// Disconnects all active calls.
		/// </summary>
		public void HangupAll()
		{
			Codec.EnqueueCommand("hangup all");
			Codec.Log(eSeverity.Informational, "Disconnecting all active calls");
		}

		/// <summary>
		/// Disconnects all active video calls.
		/// </summary>
		public void HangupAllVideo()
		{
			Codec.EnqueueCommand("hangup video");
			Codec.Log(eSeverity.Informational, "Disconnecting all active video calls");
		}

		/// <summary>
		/// Dials the contact with the given name.
		/// </summary>
		/// <param name="contactName"></param>
		public void DialAddressbook(string contactName)
		{
			if (contactName == null)
				throw new ArgumentNullException("contactName");

			contactName = StringUtils.Enquote(contactName);

			Codec.EnqueueCommand("dial addressbook {0}", contactName);
			Codec.Log(eSeverity.Informational, "Dialing addressbook contact {0}", StringUtils.ToRepresentation(contactName));
		}

		/// <summary>
		/// Dials a video call number of type h323.
		/// </summary>
		/// <param name="number"></param>
		public void DialAuto(string number)
		{
			if (number == null)
				throw new ArgumentNullException("number");

			number = StringUtils.Enquote(number);

			Codec.EnqueueCommand("dial auto {0}", number);
			Codec.Log(eSeverity.Informational, "Dialing auto number {0}", StringUtils.ToRepresentation(number));
		}

		/// <summary>
		/// Dials a video call number of type h323.
		/// Use dial manual when you do not want automatic call rollover or when
		/// the dialstring might not convey the intended transport.
		/// </summary>
		/// <param name="number"></param>
		public void DialManual(string number)
		{
			if (number == null)
				throw new ArgumentNullException("number");

			number = StringUtils.Enquote(number);

			Codec.EnqueueCommand("dial manual {0}", number);
			Codec.Log(eSeverity.Informational, "Dialing manual number {0}", StringUtils.ToRepresentation(number));
		}

		/// <summary>
		/// Dials a video call number.
		/// Use dial manual when you do not want automatic call rollover or when
		/// the dialstring might not convey the intended transport.
		/// </summary>
		/// <param name="number"></param>
		/// <param name="type"></param>
		public void DialManual(string number, eDialType type)
		{
			if (number == null)
				throw new ArgumentNullException("number");

			number = StringUtils.Enquote(number);

			string typeName = s_TypeNames.GetValue(type);

			Codec.EnqueueCommand("dial manual {0} {1}", number, typeName);
			Codec.Log(eSeverity.Informational, "Dialing manual number {0} type {1}", StringUtils.ToRepresentation(number), typeName);
		}

		/// <summary>
		/// Dials the given phone number.
		/// </summary>
		/// <param name="protocol"></param>
		/// <param name="number"></param>
		public void DialPhone(eDialProtocol protocol, string number)
		{
			if (number == null)
				throw new ArgumentNullException("number");

			number = StringUtils.Enquote(number);

			string protocolName = s_ProtocolNames.GetValue(protocol);

			Codec.EnqueueCommand("dial phone {0} {1}", protocolName, number);
			Codec.Log(eSeverity.Informational, "Dialing phone number {0} {1}", protocolName, StringUtils.ToRepresentation(number));
		}

		/// <summary>
		/// Generates the DTMF dialing tone.
		/// </summary>
		/// <param name="tone"></param>
		public void Gendial(char tone)
		{
			Codec.EnqueueCommand("gendial {0}", tone);
			Codec.Log(eSeverity.Informational, "Sending DTMF {0}", tone);
		}

		#endregion

		#region Feedback

		/// <summary>
		/// Called when we get a cs feedback message.
		/// </summary>
		/// <param name="data"></param>
		private void HandleCallState(string data)
		{
			// cs: call[34] chan[0] dialstr[192.168.1.103] state[ALLOCATED]
			// cs: call[34] chan[0] dialstr[192.168.1.103] state[RINGING]
			// cs: call[34] chan[0] dialstr[192.168.1.103] state[COMPLETE]

			int callId = CallStatus.GetCallIdFromCallState(data);
			UpdateCallState(callId, cs => cs.SetCallState(data));
		}

		/// <summary>
		/// Called when we get an active feedback message.
		/// </summary>
		/// <param name="data"></param>
		private void HandleActiveCall(string data)
		{
			// active: call[34] speed [384]

			int callId = CallStatus.GetCallIdFromActiveCall(data);
			UpdateCallState(callId, cs => cs.SetActiveCall(data));
		}

		/// <summary>
		/// Called when we get a cleared feedback message.
		/// </summary>
		/// <param name="data"></param>
		private void HandleClearedCall(string data)
		{
			// cleared: call[34]

			int callId = CallStatus.GetCallIdFromClearedCall(data);
			UpdateCallState(callId, cs => cs.SetClearedCall(data));
		}

		/// <summary>
		/// Called when we get an ended feedback message.
		/// </summary>
		/// <param name="data"></param>
		private void HandleEndedCall(string data)
		{
			// ended: call[34]

			int callId = CallStatus.GetCallIdFromEndedCall(data);
			UpdateCallState(callId, cs => cs.SetEndedCall(data));
		}

		/// <summary>
		/// Called when we get a range of callinfo feedback messages.
		/// </summary>
		/// <param name="range"></param>
		private void HandleCallRangeInfo(IEnumerable<string> range)
		{
			// callinfo begin
			// callinfo:43:Polycom Group Series Demo:192.168.1.101:384:connected:notmuted:outgoing:videocall
			// callinfo:36:192.168.1.102:256:connected:muted:outgoing:videocall
			// callinfo end

			IcdHashSet<int> ids = new IcdHashSet<int>();

			foreach (string data in range)
			{
				int callId = CallStatus.GetCallIdFromCallInfo(data);
				string data1 = data;

				UpdateCallState(callId, cs => cs.SetCallInfo(data1));

				ids.Add(callId);
			}

			// Remove any calls that are no longer in the list
			IEnumerable<int> remove = m_CallStatesSection.Execute(() => m_CallStates.Keys.Except(ids).ToArray());
			RemoveCallStatuses(remove);
		}

		/// <summary>
		/// Called when we get a notification feedback message.
		/// </summary>
		/// <param name="data"></param>
		private void HandleNotification(string data)
		{
			if (data.StartsWith("notification:callstatus:"))
			{
				// notification:callstatus:outgoing:34:Polycom Group Series Demo:192.168.1.101:connected:384:0:videocall
				int callId = CallStatus.GetCallIdFromCallStatus(data);
				UpdateCallState(callId, cs => cs.SetCallStatus(data));
			}
			else if (data.StartsWith("notification:linestatus:"))
			{
				// notification:linestatus:outgoing:32:0:0:disconnected
				int callId = CallStatus.GetCallIdFromLineStatus(data);
				UpdateCallState(callId, cs => cs.SetLineStatus(data));
			}
		}

		/// <summary>
		/// Performs the given update action for the call state with the given id.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="update"></param>
		private void UpdateCallState(int id, Action<CallStatus> update)
		{
			m_CallStatesSection.Enter();

			try
			{
				CallStatus callStatus;
				if (!m_CallStates.TryGetValue(id, out callStatus))
					callStatus = new CallStatus();

				update(callStatus);

				IcdConsole.PrintLine(eConsoleColor.Yellow, "{0}", callStatus);

				if (callStatus.ConnectionState == eConnectionState.Disconnected)
					m_CallStates.Remove(id);
				else if (callStatus.ConnectionState != eConnectionState.Inactive)
					m_CallStates[callStatus.CallId] = callStatus;
			}
			finally
			{
				m_CallStatesSection.Leave();
			}

			OnCallStatesChanged.Raise(this);
		}

		/// <summary>
		/// Removes the call states with the given ids.
		/// </summary>
		/// <param name="ids"></param>
		private void RemoveCallStatuses(IEnumerable<int> ids)
		{
			bool changed =
				m_CallStatesSection.Execute(() => ids.Aggregate(false, (current, id) => current || m_CallStates.Remove(id)));

			if (changed)
				OnCallStatesChanged.Raise(this);
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

			yield return new ConsoleCommand("AnswerVideo", "Answers the incoming video call", () => AnswerVideo());
			yield return new GenericConsoleCommand<int>("HangupVideo", "HangupVideo <CALL>", c => HangupVideo(c));
			yield return new ConsoleCommand("HangupAll", "Disconnects all active calls", () => HangupAll());
			yield return new ConsoleCommand("HangupAllVideo", "Disconnects all active video calls", () => HangupAllVideo());

			yield return new GenericConsoleCommand<string>("DialAddressbook", "DialAddressbook <NAME>", n => DialAddressbook(n));

			string protocolValues = StringUtils.ArrayFormat(EnumUtils.GetValues<eDialProtocol>());
			string typeValues = StringUtils.ArrayFormat(EnumUtils.GetValues<eDialType>());

			yield return new GenericConsoleCommand<string>("DialAuto", "DialAuto <NUMBER>", n => DialAuto(n));
			yield return new GenericConsoleCommand<string>("DialManual", "DialManual <NUMBER>", n => DialManual(n));

			string dialManualTypeHelp = string.Format("DialManualType <NUMBER> <{0}>", typeValues);
			yield return new GenericConsoleCommand<string, eDialType>("DialManualType", dialManualTypeHelp, (n, t) => DialManual(n, t));

			string dialPhoneHelp = string.Format("DialPhone <{0}> <NUMBER>", protocolValues);
			yield return new GenericConsoleCommand<eDialProtocol, string>("DialPhone", dialPhoneHelp, (p, n) => DialPhone(p, n));

			yield return new GenericConsoleCommand<char>("Gendial", "Gendial <CHAR>", c => Gendial(c));
		}

		/// <summary>
		/// Shim to avoid "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
