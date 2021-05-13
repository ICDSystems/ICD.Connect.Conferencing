using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Xml;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Conference
{
	public enum eCallRecordingStatus
	{
		/// <summary>
		/// Recording is not available.
		/// </summary>
		None = 0,

		/// <summary>
		/// The recording is ongoing.
		/// </summary>
		Recording = 1,

		/// <summary>
		/// The recording is paused.
		/// </summary>
		Paused = 2
	}

	public static class CallRecordingStatusExtensions
	{
		/// <summary>
		/// Can only start if no recording is currently ongoing or paused.
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static bool CanStart(this eCallRecordingStatus extends)
		{
			return extends == eCallRecordingStatus.None;
		}

		/// <summary>
		/// Can only stop if there is an ongoing or paused recording.
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static bool CanStop(this eCallRecordingStatus extends)
		{
			return extends != eCallRecordingStatus.None;
		}

		/// <summary>
		/// Can only pause if there is an ongoing recording.
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static bool CanPause(this eCallRecordingStatus extends)
		{
			return extends == eCallRecordingStatus.Recording;
		}

		/// <summary>
		/// Can only resume if there is a paused recording.
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static bool CanResume(this eCallRecordingStatus extends)
		{
			return extends == eCallRecordingStatus.Paused;
		}
	}

	public sealed class ConferenceComponent : AbstractCiscoComponent
	{
		#region Events

		public event EventHandler<GenericEventArgs<eCallRecordingStatus>> OnCallRecordingStatusChanged;

		#endregion

		#region Fields

		private eCallRecordingStatus m_CallRecordingStatus;

		#endregion

		#region Properties

		/// <summary>
		/// The current call recording state.
		/// </summary>
		public eCallRecordingStatus CallRecordingStatus
		{
			get { return m_CallRecordingStatus; }
			private set
			{
				if (m_CallRecordingStatus == value)
					return;

				m_CallRecordingStatus = value;
				Codec.Logger.Log(eSeverity.Informational, "Call recording status set to: {0}", m_CallRecordingStatus);
				OnCallRecordingStatusChanged.Raise(this, m_CallRecordingStatus);
			}
		}

		#endregion

		#region Constructor

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="codec"></param>
		public ConferenceComponent(CiscoCodecDevice codec)
			: base(codec)
		{
			Subscribe(codec);
		}

		#endregion

		#region Methods

		/// <summary>
		/// Sends the raise hand command.
		/// </summary>
		/// <param name="callId"></param>
		public void RaiseHand(int callId)
		{
			Codec.SendCommand("xCommand Conference Hand Raise CallId: {0}", callId);
			Codec.Logger.Log(eSeverity.Informational, "Raising Hand for call {0}", callId);
		}

		/// <summary>
		/// Sends the lower hand command.
		/// </summary>
		/// <param name="callId"></param>
		public void LowerHand(int callId)
		{
			Codec.SendCommand("xCommand Conference Hand Lower CallId: {0}", callId);
			Codec.Logger.Log(eSeverity.Informational, "Lowering Hand for call {0}", callId);
		}

		/// <summary>
		/// Starts recording the call.
		/// </summary>
		/// <param name="callId"></param>
		public void RecordingStart(int callId)
		{
			Codec.SendCommand("xCommand Conference Recording Start CallId: {0}", callId);
			Codec.Logger.Log(eSeverity.Informational, "Starting Recording for call {0}", callId);
		}

		/// <summary>
		/// Stops recording the call.
		/// </summary>
		/// <param name="callId"></param>
		public void RecordingStop(int callId)
		{
			Codec.SendCommand("XCommand Conference Recording Stop CallId: {0}", callId);
			Codec.Logger.Log(eSeverity.Informational, "Stopping Recording for call {0}", callId);
		}

		/// <summary>
		/// Pauses recording the call.
		/// </summary>
		/// <param name="callId"></param>
		public void RecordingPause(int callId)
		{
			Codec.SendCommand("xCommand Conference Recording Pause CallId: {0}", callId);
			Codec.Logger.Log(eSeverity.Informational, "Pausing Recording for call {0}", callId);
		}

		/// <summary>
		/// Resumes recording the call.
		/// </summary>
		/// <param name="callId"></param>
		public void RecordingResume(int callId)
		{
			Codec.SendCommand("xCommand Conference Recording Resume CallId: {0}", callId);
			Codec.Logger.Log(eSeverity.Informational, "Resuming Recording for call {0}", callId);
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

			codec.RegisterParserCallback(ParseCallRecordingStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "Conference",
			                             "Call", "Recording");
		}

		/// <summary>
		/// Unsubscribes from the codec events.
		/// </summary>
		/// <param name="codec"></param>
		protected override void Unsubscribe(CiscoCodecDevice codec)
		{
			base.Unsubscribe(codec);

			codec.UnregisterParserCallback(ParseCallRecordingStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "Conference",
			                               "Call", "Recording");
		}

		private void ParseCallRecordingStatus(CiscoCodecDevice codec, string resultid, string xml)
		{
			string content = XmlUtils.GetInnerXml(xml);
			CallRecordingStatus = EnumUtils.Parse<eCallRecordingStatus>(content, true);
		}

		#endregion

		#region Console

		public override string ConsoleName { get { return "ConferenceComponent"; } }

		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Call Recording Status", CallRecordingStatus);
		}

		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<int>("RaiseHand", "RaiseHand {CallId}", i => RaiseHand(i));
			yield return new GenericConsoleCommand<int>("LowerHand", "LowerHand {CallId}", i => LowerHand(i));
		}

		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}