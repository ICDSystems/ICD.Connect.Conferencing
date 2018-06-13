using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Dial
{
	public sealed class DialComponent : AbstractPolycomComponent
	{
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

		private readonly Dictionary<int, CallState> m_CallStates;
		private readonly SafeCriticalSection m_CallStatesSection;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="codec"></param>
		public DialComponent(PolycomGroupSeriesDevice codec)
			: base(codec)
		{
			m_CallStates = new Dictionary<int, CallState>();
			m_CallStatesSection = new SafeCriticalSection();

			Subscribe(Codec);

			Codec.RegisterFeedback("cs", HandleCallState);
			Codec.RegisterFeedback("active", HandleActiveCall);
			Codec.RegisterFeedback("cleared", HandleClearedCall);
			Codec.RegisterFeedback("ended", HandleEndedCall);
			Codec.RegisterFeedback("callinfo", HandleCallInfo);
			Codec.RegisterFeedback("notification", HandleNotification);

			if (Codec.Initialized)
				Initialize();
		}

		/// <summary>
		/// Called to initialize the component.
		/// </summary>
		protected override void Initialize()
		{
			base.Initialize();

			Codec.SendCommand("callstate register");
			Codec.SendCommand("notify callstatus");
			Codec.SendCommand("notify linestatus");
			Codec.SendCommand("listen video");

			Codec.SendCommand("callinfo all");
			Codec.SendCommand("callstate get");
		}

		#region Methods

		/// <summary>
		/// Answers the incoming video call.
		/// </summary>
		public void AnswerVideo()
		{
			Codec.SendCommand("answer video");
			Codec.Log(eSeverity.Informational, "Answering incoming video call");
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

			Codec.SendCommand("dial addressbook {0}", contactName);
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

			Codec.SendCommand("dial auto auto {0}", number);
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

			Codec.SendCommand("dial manual auto {0}", number);
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

			Codec.SendCommand("dial manual auto {0} {1}", number, typeName);
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

			Codec.SendCommand("dial phone {0} {1}", protocolName, number);
			Codec.Log(eSeverity.Informational, "Dialing phone number {0} {1}", protocolName, StringUtils.ToRepresentation(number));
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

			int callId = CallState.GetCallIdFromCallState(data);
			UpdateCallState(callId, cs => cs.SetCallState(data));
		}

		/// <summary>
		/// Called when we get an active feedback message.
		/// </summary>
		/// <param name="data"></param>
		private void HandleActiveCall(string data)
		{
			// active: call[34] speed [384]

			int callId = CallState.GetCallIdFromActiveCall(data);
			UpdateCallState(callId, cs => cs.SetActiveCall(data));
		}

		/// <summary>
		/// Called when we get a cleared feedback message.
		/// </summary>
		/// <param name="data"></param>
		private void HandleClearedCall(string data)
		{
			// cleared: call[34]

			int callId = CallState.GetCallIdFromClearedCall(data);
			UpdateCallState(callId, cs => cs.SetClearedCall(data));
		}

		/// <summary>
		/// Called when we get an ended feedback message.
		/// </summary>
		/// <param name="data"></param>
		private void HandleEndedCall(string data)
		{
			// ended: call[34]

			int callId = CallState.GetCallIdFromEndedCall(data);
			UpdateCallState(callId, cs => cs.SetEndedCall(data));
		}

		/// <summary>
		/// Called when we get a callinfo feedback message.
		/// </summary>
		/// <param name="data"></param>
		private void HandleCallInfo(string data)
		{
			// callinfo begin
			// callinfo:43:Polycom Group Series Demo:192.168.1.101:384:connected:notmuted:outgoing:videocall
			// callinfo:36:192.168.1.102:256:connected:muted:outgoing:videocall
			// callinfo end

			int callId = CallState.GetCallIdFromCallInfo(data);
			UpdateCallState(callId, cs => cs.SetCallInfo(data));
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
				int callId = CallState.GetCallIdFromCallStatus(data);
				UpdateCallState(callId, cs => cs.SetCallStatus(data));
			}
			else if (data.StartsWith("notification:callstatus:"))
			{
				// notification:linestatus:outgoing:32:0:0:disconnected
				int callId = CallState.GetCallIdFromLineStatus(data);
				UpdateCallState(callId, cs => cs.SetLineStatus(data));
			}
		}

		/// <summary>
		/// Performs the given update action for the call state with the given id.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="update"></param>
		private void UpdateCallState(int id, Action<CallState> update)
		{
			m_CallStatesSection.Enter();

			try
			{
				CallState callState;
				if (!m_CallStates.TryGetValue(id, out callState))
					callState = new CallState();

				update(callState);

				m_CallStates.Remove(id);
				if (callState.Connected)
					m_CallStates[callState.CallId] = callState;
			}
			finally
			{
				m_CallStatesSection.Leave();
			}
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

			yield return new GenericConsoleCommand<string>("DialAddressbook", "DialAddressbook <NAME>", n => DialAddressbook(n));

			string protocolValues = StringUtils.ArrayFormat(EnumUtils.GetValues<eDialProtocol>());
			string typeValues = StringUtils.ArrayFormat(EnumUtils.GetValues<eDialType>());

			yield return new GenericConsoleCommand<string>("DialAuto", "DialAuto <NUMBER>", n => DialAuto(n));
			yield return new GenericConsoleCommand<string>("DialManual", "DialManual <NUMBER>", n => DialManual(n));

			string dialManualTypeHelp = string.Format("DialManualType <NUMBER> <{0}>", typeValues);
			yield return new GenericConsoleCommand<string, eDialType>("DialManualType", dialManualTypeHelp, (n, t) => DialManual(n, t));

			string dialPhoneHelp = string.Format("DialPhone <{0}> <NUMBER>", protocolValues);
			yield return new GenericConsoleCommand<eDialProtocol, string>("DialPhone", dialPhoneHelp, (p, n) => DialPhone(p, n));
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
