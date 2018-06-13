﻿using ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Dial;
using NUnit.Framework;

namespace ICD.Connect.Conferencing.Polycom.Tests.Devices.Codec.Components.Dial
{
	[TestFixture]
	public sealed class CallStateTest
	{
		#region Methods

		public void SetCallInfoTest(string callInfo)
		{
			Assert.Inconclusive();
		}

		public void SetCallState(string callState)
		{
			Assert.Inconclusive();
		}

		public void SetLineStatus(string lineStatus)
		{
			Assert.Inconclusive();
		}

		[TestCase("callinfo:43:Polycom Group Series Demo:192.168.1.101:384:connected:notmuted:outgoing:videocall", 43)]
		[TestCase("callinfo:43:192.168.1.101:384:connected:notmuted:outgoing:videocall", 43)]
		public static void GetCallIdFromCallInfoTest(string callInfo, int expected)
		{
			Assert.AreEqual(expected, CallState.GetCallIdFromCallInfo(callInfo));
		}

		[TestCase("cs: call[34] chan[0] dialstr[192.168.1.103] state[ALLOCATED]", 34)]
		public static void GetCallIdFromCallStateTest(string callState, int expected)
		{
			Assert.AreEqual(expected, CallState.GetCallIdFromCallState(callState));
		}

		[TestCase("notification:linestatus:outgoing:32:0:0:disconnected", 32)]
		public static void GetCallIdFromLineStatusTest(string lineStatus, int expected)
		{
			Assert.AreEqual(expected, CallState.GetCallIdFromLineStatus(lineStatus));
		}

		#endregion
	}
}
