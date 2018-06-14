using ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Dial;
using NUnit.Framework;

namespace ICD.Connect.Conferencing.Polycom.Tests.Devices.Codec.Components.Dial
{
	[TestFixture]
	public sealed class CallStatusTest
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

		public void SetCallStatus(string callStatus)
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
			Assert.AreEqual(expected, CallStatus.GetCallIdFromCallInfo(callInfo));
		}

		[TestCase("cs: call[34] chan[0] dialstr[192.168.1.103] state[ALLOCATED]", 34)]
		public static void GetCallIdFromCallStateTest(string callState, int expected)
		{
			Assert.AreEqual(expected, CallStatus.GetCallIdFromCallState(callState));
		}

		[TestCase("notification:callstatus:outgoing:34:Polycom Group Series Demo:192.168.1.101:connected:384:0:videocall", 34)]
		public static void GetCallIdFromCallStatusTest(string callStatus, int expected)
		{
			Assert.AreEqual(expected, CallStatus.GetCallIdFromCallStatus(callStatus));
		}

		[TestCase("notification:linestatus:outgoing:32:0:0:disconnected", 32)]
		public static void GetCallIdFromLineStatusTest(string lineStatus, int expected)
		{
			Assert.AreEqual(expected, CallStatus.GetCallIdFromLineStatus(lineStatus));
		}

		[TestCase("active: call[34] speed [384]", 34)]
		public static void GetCallIdFromActiveCallTest(string activeCall, int expected)
		{
			Assert.AreEqual(expected, CallStatus.GetCallIdFromActiveCall(activeCall));
		}

		[TestCase("cleared: call[34]", 34)]
		public static void GetCallIdFromClearedCallTest(string clearedCall, int expected)
		{
			Assert.AreEqual(expected, CallStatus.GetCallIdFromClearedCall(clearedCall));
		}

		[TestCase("ended: call[34]", 34)]
		public static void GetCallIdFromEndedCallTest(string endedCall, int expected)
		{
			Assert.AreEqual(expected, CallStatus.GetCallIdFromEndedCall(endedCall));
		}

		#endregion
	}
}
