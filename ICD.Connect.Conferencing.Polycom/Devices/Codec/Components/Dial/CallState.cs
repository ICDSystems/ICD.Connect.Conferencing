using System;
using System.Text.RegularExpressions;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Dial
{
	public sealed class CallState
	{
		private const string CALL_INFO_REGEX =
			@"callinfo:(?'call'\d+):((?'name'[^:]+):)?(?'number'[^:]+):(?'speed'[^:]+):(?'connected'[^:]+):(?'muted'[^:]+):(?'outgoing'[^:]+):(?'video'[^:]+)";

		private const string CALL_STATE_REGEX =
			@"cs: call\[(?'call'\d+)\] chan\[(?'chan'\d+)\] dialstr\[(?'dialstr'.+)\] state\[(?'state'.+)\]";

		private const string LINE_STATUS_REGEX =
			@"notification:linestatus:(?'direction'[^:]+):(?'callId'[^:]+):(?'lineId'[^:]+):(?'channelId'[^:]+):(?'status'[^:]+)";

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

			// State = match.Groups["state"].Value;
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
		/// Gets the call id from the given call info data.
		/// 
		/// callinfo:[callid]:[far site name]:[far site number]:[speed]:[connection status]:[mute status]:[call direction]:[call type]
		/// callinfo:43:Polycom Group Series Demo:192.168.1.101:384:connected:notmuted:outgoing:videocall
		/// </summary>
		/// <param name="callInfo"></param>
		public static int GetCallIdFromCallInfo(string callInfo)
		{
			if (callInfo == null)
				throw new ArgumentNullException("callInfo");

			CallState instance = new CallState();
			instance.SetCallInfo(callInfo);

			return instance.CallId;
		}

		/// <summary>
		/// Gets the call id from the given call state.
		/// 
		/// cs: call[34] chan[0] dialstr[192.168.1.103] state[ALLOCATED]
		/// </summary>
		/// <param name="callState"></param>
		public static int GetCallIdFromCallState(string callState)
		{
			if (callState == null)
				throw new ArgumentNullException("callState");

			CallState instance = new CallState();
			instance.SetCallState(callState);

			return instance.CallId;
		}

		/// <summary>
		/// Gets the call id from the given line status data.
		/// 
		/// notification:linestatus:[direction]:[call id]:[line id]:[channelid]:[connection status]
		/// notification:linestatus:outgoing:32:0:0:disconnected
		/// </summary>
		/// <param name="lineStatus"></param>
		public static int GetCallIdFromLineStatus(string lineStatus)
		{
			if (lineStatus == null)
				throw new ArgumentNullException("lineStatus");

			CallState instance = new CallState();
			instance.SetLineStatus(lineStatus);

			return instance.CallId;
		}

		#endregion
	}
}
