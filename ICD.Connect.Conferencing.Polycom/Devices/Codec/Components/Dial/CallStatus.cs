using System;
using System.Text.RegularExpressions;
using ICD.Common.Utils.Collections;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Dial
{
	public sealed class CallStatus
	{
		private const string CALL_INFO_REGEX =
			@"callinfo:(?'call'\d+):((?'name'[^:]+):)?(?'number'[^:]+):(?'speed'[^:]+):(?'connected'[^:]+):(?'muted'[^:]+):(?'outgoing'[^:]+):(?'video'[^:]+)";

		private const string CALL_STATE_REGEX =
			@"cs: call\[(?'call'\d+)\] chan\[(?'chan'\d+)\] dialstr\[(?'dialstr'.+)\] state\[(?'state'.+)\]";

		private const string CALL_STATUS_REGEX =
			@"notification:callstatus:(?'direction'[^:]+):(?'call'[^:]+):(?'name'[^:]+):(?'number'[^:]+):(?'connected'[^:]+):(?'speed'[^:]+):[^:]+:(?'type'[^:]+)";

		private const string LINE_STATUS_REGEX =
			@"notification:linestatus:(?'direction'[^:]+):(?'callId'[^:]+):(?'lineId'[^:]+):(?'channelId'[^:]+):(?'status'[^:]+)";

		private const string ACTIVE_CALL_REGEX = @"active: call\[(?'call'\d+)\] speed \[(?'speed'[^]]+)\]";
		private const string ENDED_CALL_REGEX = @"ended: call\[(?'call'\d+)\]";
		private const string CLEARED_CALL_REGEX = @"cleared: call\[(?'call'\d+)\]";

		private static readonly BiDictionary<eCallState, string> s_CallStateNames =
			new BiDictionary<eCallState, string>
			{
				{eCallState.Allocated, "ALLOCATED"},
				{eCallState.Ringing, "RINGING"},
				{eCallState.Connected, "CONNECTED"},
				{eCallState.Complete, "COMPLETE"}
			};

		#region Properties

		public int CallId { get; set; }

		public int LineId { get; set; }

		public int ChannelId { get; set; }

		public string FarSiteName { get; set; }

		public string FarSiteNumber { get; set; }

		public bool Connected { get; set; }

		public bool Muted { get; set; }

		public bool Outgoing { get; set; }

		public string Speed { get; set; }

		public bool VideoCall { get; set; }

		public eCallState State { get; set; }

		#endregion

		#region Methods

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
			FarSiteNumber = match.Groups["number"].Value;
			Speed = match.Groups["speed"].Value;

			// Polycom documentation doesn't give us an exhaustive list of these, but they seem boolean?
			Connected = match.Groups["connected"].Value == "connected";
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
			FarSiteNumber = match.Groups["dialstr"].Value;

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
			FarSiteNumber = match.Groups["number"].Value;
			Speed = match.Groups["speed"].Value;

			// Polycom documentation doesn't give us an exhaustive list of these, but they seem boolean?
			Outgoing = match.Groups["direction"].Value == "outgoing";
			Connected = match.Groups["connected"].Value == "connected";
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

			CallId = int.Parse(match.Groups["callId"].Value);
			LineId = int.Parse(match.Groups["lineId"].Value);
			ChannelId = int.Parse(match.Groups["channelId"].Value);

			// Polycom documentation doesn't give us an exhaustive list of these, but they seem boolean?
			Outgoing = match.Groups["direction"].Value == "outgoing";
			Connected = match.Groups["status"].Value == "connected";
		}

		/// <summary>
		/// Updates the call state with the given active call data.
		/// 
		/// active: call[34] speed [384]
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

			Connected = true;
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

			Connected = false;
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

			Connected = false;
		}

		#endregion

		#region Static Methods

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
