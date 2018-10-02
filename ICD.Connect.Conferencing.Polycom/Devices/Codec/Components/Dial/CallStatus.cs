using System;
using System.Linq;
using System.Text.RegularExpressions;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Dial
{
	public sealed class CallStatus
	{
		private const string CALL_INFO_REGEX =
			@"callinfo:(?'call'\d+):((?'name'[^:]*):)?(?'number'[^:]+):(?'speed'[^:]+):(?'connected'[^:]+):(?'muted'[^:]+):(?'outgoing'[^:]+):(?'video'[^:]+)";

		private const string CALL_STATE_REGEX =
			@"cs: call\[(?'call'\d+)\] chan\[(?'chan'\d+)\] dialstr\[(?'dialstr'.+)\] state\[(?'state'.+)\]";

		private const string CALL_STATUS_REGEX =
			@"notification:callstatus:(?'direction'[^:]+):(?'call'[^:]+):(?'name'[^:]*):(?'number'[^:]+):(?'connected'[^:]+):(?'speed'[^:]+):[^:]+:(?'type'[^:]+)";

		private const string LINE_STATUS_REGEX =
			@"notification:linestatus:(?'direction'[^:]+)(:(?'number'[^:]*))?:(?'callId'\d+):(?'lineId'\d+):(?'channelId'\d+):(?'status'[^:]+)";

		private const string ACTIVE_CALL_REGEX = @"active: call\[(?'call'\d+)\] speed\[(?'speed'[^]]+)\]";
		private const string ENDED_CALL_REGEX = @"ended: call\[(?'call'\d+)\]";
		private const string CLEARED_CALL_REGEX = @"cleared: call\[(?'call'\d+)\]";

		private static readonly BiDictionary<eCallState, string> s_CallStateNames =
			new BiDictionary<eCallState, string>
			{
				{eCallState.Allocated, "ALLOCATED"},
				{eCallState.Ringing, "RINGING"},
				{eCallState.Connecting, "CONNECTING"},
				{eCallState.Connected, "CONNECTED"},
				{eCallState.Complete, "COMPLETE"}
			};

		private static readonly BiDictionary<eConnectionState, string> s_ConnectionStateNames =
			new BiDictionary<eConnectionState, string>
			{
				{eConnectionState.Opened, "opened"},
				{eConnectionState.Ringing, "ringing"},
				{eConnectionState.Connecting, "connecting"},
				{eConnectionState.Connected, "connected"},
				{eConnectionState.Inactive, "inactive"},
				{eConnectionState.Disconnecting, "disconnecting"},
				{eConnectionState.Disconnected, "disconnected"},
			};

		private eConnectionState m_ConnectionState;

		#region Properties

		public int CallId { get; set; }

		public int LineId { get; set; }

		public int ChannelId { get; set; }

		public string FarSiteName { get; set; }

		public string FarSiteNumber { get; set; }

		public bool Muted { get; set; }

		public bool? Outgoing { get; set; }

		public string Speed { get; set; }

		public bool VideoCall { get; set; }

		public eCallState State { get; set; }

		public eConnectionState ConnectionState
		{
			get { return m_ConnectionState; }
			set
			{
				// Line status and call status conflict a little
				// For example Connecting may return to Ringing, Connected may return to Connecting.
				// This is possibly a race condition on the Polycom side.
				//
				// For now I'm adding some simple checks to prevent the source status from flickering.
				// Maybe this is better solved with a state machine, I dunno.
					switch (value)
					{
						case eConnectionState.Unknown:
							break;
						case eConnectionState.Opened:
							break;
						case eConnectionState.Ringing:
							if (m_ConnectionState == eConnectionState.Connecting)
								return;
							break;
						case eConnectionState.Connecting:
							if (m_ConnectionState == eConnectionState.Connected)
								return;
							break;
						case eConnectionState.Connected:
							break;
						case eConnectionState.Inactive:
							if (m_ConnectionState == eConnectionState.Disconnected)
								return;
							break;
						case eConnectionState.Disconnecting:
							if (m_ConnectionState == eConnectionState.Disconnected)
								return;
							break;
						case eConnectionState.Disconnected:
							break;
						default:
							throw new ArgumentOutOfRangeException("value");
					}

				m_ConnectionState = value;
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Gets a string representation for this instance.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			ReprBuilder builder = new ReprBuilder(this);

			if (CallId != 0)
				builder.AppendProperty("CallId", CallId);

			if (LineId != 0)
				builder.AppendProperty("LineId", LineId);

			if (ChannelId != 0)
				builder.AppendProperty("ChannelId", LineId);

			if (!string.IsNullOrEmpty(FarSiteName))
				builder.AppendProperty("FarSiteName", FarSiteName);

			if (!string.IsNullOrEmpty(FarSiteNumber))
				builder.AppendProperty("FarSiteNumber", FarSiteNumber);

			if (ConnectionState != eConnectionState.Unknown)
				builder.AppendProperty("ConnectionState", ConnectionState);

			if (Muted)
				builder.AppendProperty("Muted", Muted);

			if (Outgoing != null)
				builder.AppendProperty("Outgoing", Outgoing);

			if (!string.IsNullOrEmpty(Speed))
				builder.AppendProperty("Speed", Speed);

			if (VideoCall)
				builder.AppendProperty("VideoCall", VideoCall);

			if (State != eCallState.Unknown)
				builder.AppendProperty("State", State);

			return builder.ToString();
		}

		/// <summary>
		/// Updates the call state with the given call info data.
		/// 
		/// callinfo:[callid]:[far site name]:[far site number]:[speed]:[connection status]:[mute status]:[call direction]:[call type]
		/// callinfo:43:Polycom Group Series Demo:192.168.1.101:384:connected:notmuted:outgoing:videocall
		/// </summary>
		/// <param name="callInfo"></param>
		public void SetCallInfo(string callInfo)
		{
			if (callInfo == null)
				throw new ArgumentNullException("callInfo");

			Match match = Regex.Match(callInfo, CALL_INFO_REGEX);

			if (!match.Success)
				throw new ArgumentException("Unable to parse call info", "callInfo");

			CallId = int.Parse(match.Groups["call"].Value);
			FarSiteName = match.Groups["name"].Value;
			FarSiteNumber = CleanupNumber(match.Groups["number"].Value);
			Speed = match.Groups["speed"].Value;

			string stateName = match.Groups["connected"].Value;
			
			eConnectionState state;
			ConnectionState = s_ConnectionStateNames.TryGetKey(stateName, out state) ? state : eConnectionState.Unknown;

			// Polycom documentation doesn't give us an exhaustive list of these, but they seem boolean?
			Muted = match.Groups["muted"].Value == "muted";
			Outgoing = match.Groups["outgoing"].Value == "outgoing";
			VideoCall = match.Groups["video"].Value == "videocall";
		}

		/// <summary>
		/// Updates the call state with the given call state data.
		/// 
		/// cs: call[34] chan[0] dialstr[192.168.1.103] state[ALLOCATED]
		/// </summary>
		/// <param name="callState"></param>
		public void SetCallState(string callState)
		{
			if (callState == null)
				throw new ArgumentNullException("callState");

			Match match = Regex.Match(callState, CALL_STATE_REGEX);

			if (!match.Success)
				throw new ArgumentException("Unable to parse call state", "callState");

			CallId = int.Parse(match.Groups["call"].Value);
			ChannelId = int.Parse(match.Groups["chan"].Value);
			FarSiteNumber = CleanupNumber(match.Groups["dialstr"].Value);

			string stateName = match.Groups["state"].Value;

			eCallState state;
			State = s_CallStateNames.TryGetKey(stateName, out state) ? state : eCallState.Unknown;
		}

		/// <summary>
		/// Updates the call state with the given call status data.
		/// 
		/// notification:callstatus:[calldirection]:[call id]:[far sitename]:[far sitenumber]:[connectionstatus]:[callspeed]:[status-specific causecode from call engine]:[calltype]
		/// notification:callstatus:outgoing:34:Polycom Group Series Demo:192.168.1.101:connected:384:0:videocall
		/// </summary>
		/// <param name="callStatus"></param>
		public void SetCallStatus(string callStatus)
		{
			if (callStatus == null)
				throw new ArgumentNullException("callStatus");

			Match match = Regex.Match(callStatus, CALL_STATUS_REGEX);

			if (!match.Success)
				throw new ArgumentException("Unable to parse call status", "callStatus");

			CallId = int.Parse(match.Groups["call"].Value);
			FarSiteName = match.Groups["name"].Value;
			FarSiteNumber = CleanupNumber(match.Groups["number"].Value);
			Speed = match.Groups["speed"].Value;

			string stateName = match.Groups["connected"].Value;

			eConnectionState state;
			ConnectionState = s_ConnectionStateNames.TryGetKey(stateName, out state) ? state : eConnectionState.Unknown;

			// Polycom documentation doesn't give us an exhaustive list of these, but they seem boolean?
			Outgoing = match.Groups["direction"].Value == "outgoing";
			VideoCall = match.Groups["type"].Value == "videocall";
		}

		/// <summary>
		/// Updates the call state with the given line status data.
		/// 
		/// notification:linestatus:[direction]:[call id]:[line id]:[channelid]:[connection status]
		/// notification:linestatus:outgoing:32:0:0:disconnected
		/// </summary>
		/// <param name="lineStatus"></param>
		public void SetLineStatus(string lineStatus)
		{
			if (lineStatus == null)
				throw new ArgumentNullException("lineStatus");

			Match match = Regex.Match(lineStatus, LINE_STATUS_REGEX);

			if (!match.Success)
				throw new ArgumentException("Unable to parse line status", "lineStatus");

			FarSiteNumber = CleanupNumber(match.Groups["number"].Value);

			CallId = int.Parse(match.Groups["callId"].Value);
			LineId = int.Parse(match.Groups["lineId"].Value);
			ChannelId = int.Parse(match.Groups["channelId"].Value);

			string stateName = match.Groups["status"].Value;

			eConnectionState state;
			ConnectionState = s_ConnectionStateNames.TryGetKey(stateName, out state) ? state : eConnectionState.Unknown;

			// NOTE - Don't use line status for call direction, incoming calls show as "outgoing". Brillant!
		}

		/// <summary>
		/// Updates the call state with the given active call data.
		/// 
		/// active: call[34] speed[384]
		/// </summary>
		/// <param name="activeCall"></param>
		public void SetActiveCall(string activeCall)
		{
			if (activeCall == null)
				throw new ArgumentNullException("activeCall");

			Match match = Regex.Match(activeCall, ACTIVE_CALL_REGEX);

			if (!match.Success)
				throw new ArgumentException("Unable to parse active call", "activeCall");

			CallId = int.Parse(match.Groups["call"].Value);
			Speed = match.Groups["speed"].Value;

			ConnectionState = eConnectionState.Connected;
		}

		/// <summary>
		/// Updates the call state with the given cleared call data.
		/// 
		/// cleared: call[34]
		/// </summary>
		/// <param name="clearedCall"></param>
		public void SetClearedCall(string clearedCall)
		{
			if (clearedCall == null)
				throw new ArgumentNullException("clearedCall");

			Match match = Regex.Match(clearedCall, CLEARED_CALL_REGEX);

			if (!match.Success)
				throw new ArgumentException("Unable to parse cleared call", "clearedCall");

			CallId = int.Parse(match.Groups["call"].Value);

			ConnectionState = eConnectionState.Inactive;
		}

		/// <summary>
		/// Updates the call state with the given ended call data.
		/// 
		/// ended: call[34]
		/// </summary>
		/// <param name="endedCall"></param>
		public void SetEndedCall(string endedCall)
		{
			if (endedCall == null)
				throw new ArgumentNullException("endedCall");

			Match match = Regex.Match(endedCall, ENDED_CALL_REGEX);

			if (!match.Success)
				throw new ArgumentException("Unable to parse ended call", "endedCall");

			CallId = int.Parse(match.Groups["call"].Value);

			ConnectionState = eConnectionState.Inactive;
		}

		#endregion

		#region Static Methods

		/// <summary>
		/// Removes trailing query info from a number.
		/// E.g.
		///		chris.van@profoundtech.onmicrosoft.com;gruu;opaque=app:conf:focus:id:WPBNWHOH
		/// Becomes
		///		chris.van@profoundtech.onmicrosoft.com
		/// </summary>
		/// <param name="number"></param>
		/// <returns></returns>
		public static string CleanupNumber(string number)
		{
			return number == null ? null : number.Split(';').FirstOrDefault();
		}

		/// <summary>
		/// Gets the call id from the given call info data.
		/// 
		/// callinfo:[callid]:[far site name]:[far site number]:[speed]:[connection status]:[mute status]:[call direction]:[call type]
		/// callinfo:43:Polycom Group Series Demo:192.168.1.101:384:connected:notmuted:outgoing:videocall
		/// </summary>
		/// <param name="callInfo"></param>
		/// <returns></returns>
		public static int GetCallIdFromCallInfo(string callInfo)
		{
			if (callInfo == null)
				throw new ArgumentNullException("callInfo");

			CallStatus instance = new CallStatus();
			instance.SetCallInfo(callInfo);

			return instance.CallId;
		}

		/// <summary>
		/// Gets the call id from the given call state.
		/// 
		/// cs: call[34] chan[0] dialstr[192.168.1.103] state[ALLOCATED]
		/// </summary>
		/// <param name="callState"></param>
		/// <returns></returns>
		public static int GetCallIdFromCallState(string callState)
		{
			if (callState == null)
				throw new ArgumentNullException("callState");

			CallStatus instance = new CallStatus();
			instance.SetCallState(callState);

			return instance.CallId;
		}

		/// <summary>
		/// Gets the call id from the given call status data.
		/// 
		/// notification:callstatus:[calldirection]:[call id]:[far sitename]:[far sitenumber]:[connectionstatus]:[callspeed]:[status-specific causecode from call engine]:[calltype]
		/// notification:callstatus:outgoing:34:Polycom Group Series Demo:192.168.1.101:connected:384:0:videocall
		/// </summary>
		/// <param name="callStatus"></param>
		/// <returns></returns>
		public static int GetCallIdFromCallStatus(string callStatus)
		{
			if (callStatus == null)
				throw new ArgumentNullException("callStatus");

			CallStatus instance = new CallStatus();
			instance.SetCallStatus(callStatus);

			return instance.CallId;
		}

		/// <summary>
		/// Gets the call id from the given line status data.
		/// 
		/// notification:linestatus:[direction]:[call id]:[line id]:[channelid]:[connection status]
		/// notification:linestatus:outgoing:32:0:0:disconnected
		/// </summary>
		/// <param name="lineStatus"></param>
		/// <returns></returns>
		public static int GetCallIdFromLineStatus(string lineStatus)
		{
			if (lineStatus == null)
				throw new ArgumentNullException("lineStatus");

			CallStatus instance = new CallStatus();
			instance.SetLineStatus(lineStatus);

			return instance.CallId;
		}

		/// <summary>
		/// Updates the call state with the given active call data.
		/// 
		/// active: call[34] speed [384]
		/// </summary>
		/// <param name="activeCall"></param>
		/// <returns></returns>
		public static int GetCallIdFromActiveCall(string activeCall)
		{
			if (activeCall == null)
				throw new ArgumentNullException("activeCall");

			CallStatus instance = new CallStatus();
			instance.SetActiveCall(activeCall);

			return instance.CallId;
		}

		/// <summary>
		/// Updates the call state with the given cleared call data.
		/// 
		/// cleared: call[34]
		/// </summary>
		/// <param name="clearedCall"></param>
		/// <returns></returns>
		public static int GetCallIdFromClearedCall(string clearedCall)
		{
			if (clearedCall == null)
				throw new ArgumentNullException("clearedCall");

			CallStatus instance = new CallStatus();
			instance.SetClearedCall(clearedCall);

			return instance.CallId;
		}

		/// <summary>
		/// Updates the call state with the given ended call data.
		/// 
		/// ended: call[34]
		/// </summary>
		/// <param name="endedCall"></param>
		/// <returns></returns>
		public static int GetCallIdFromEndedCall(string endedCall)
		{
			if (endedCall == null)
				throw new ArgumentNullException("endedCall");

			CallStatus instance = new CallStatus();
			instance.SetEndedCall(endedCall);

			return instance.CallId;
		}

		#endregion
	}
}
