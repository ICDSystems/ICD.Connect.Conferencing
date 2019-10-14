using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Cameras;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;

namespace ICD.Connect.Conferencing.Zoom.Components.TraditionalCall
{
	public sealed class TraditionalZoomParticipant : ITraditionalParticipant
	{
		#region Events

		/// <summary>
		/// Raised when the participant's status changes.
		/// </summary>
		public event EventHandler<ParticipantStatusEventArgs> OnStatusChanged;

		/// <summary>
		/// Raised when the participant source type changes.
		/// </summary>
		public event EventHandler<ParticipantTypeEventArgs> OnSourceTypeChanged;

		/// <summary>
		/// Raised when the participant's name changes.
		/// </summary>
		public event EventHandler<StringEventArgs> OnNameChanged;

		/// <summary>
		/// Raised when the source number changes.
		/// </summary>
		public event EventHandler<StringEventArgs> OnNumberChanged;

		#endregion

		private readonly ZoomRoom m_ZoomRoom;

		#region Properties

		public string Name { get; private set; }
		public string Number { get; private set; }
		public eCallType SourceType { get { return eCallType.Audio; } }
		public eParticipantStatus Status { get; private set; }
		public eCallDirection Direction { get; private set; }
		public DateTime? Start { get; private set; }
		public DateTime? End { get; private set; }
		public DateTime DialTime { get; private set; }
		public IRemoteCamera Camera { get; private set; }
		public string CallId { get; private set; }

		#endregion

		#region Constructor

		public TraditionalZoomParticipant(ZoomRoom zoomRoom, TraditionalZoomPhoneCallInfo info)
		{
			m_ZoomRoom = zoomRoom;
			Start = IcdEnvironment.GetLocalTime();
			Update(info);
		}

		#endregion

		#region Methods

		public void Update(TraditionalZoomPhoneCallInfo info)
		{
			CallId = info.CallId;
			Name = info.PeerDisplayName;
			Number = info.PeerNumber;
			Direction = info.IsIncomingCall ? eCallDirection.Incoming : eCallDirection.Outgoing;
			Camera = null;
			Status = info.Reason != 0 ? eParticipantStatus.Disconnected : eParticipantStatus.Connected;
		}

		public void Hold()
		{
			throw new NotSupportedException();
		}

		public void Resume()
		{
			throw new NotSupportedException();
		}

		public void Hangup()
		{
			m_ZoomRoom.SendCommand("zCommand Dial PhoneHangUp CallId: {0}", CallId);
		}

		public void SendDtmf(string data)
		{
			try
			{
				char key = char.Parse(data);

				m_ZoomRoom.SendCommand("zCommand SendSIPDTMF CallID: {0} Key: {1}", CallId, key);
			}
			catch (FormatException e)
			{
				m_ZoomRoom.Log(eSeverity.Error, e, "Cannot convert string to DTMF character.");
			}
		}

		#endregion

		#region Console

		public string ConsoleName { get { return Name; } }
		public string ConsoleHelp { get { return "Zoom conference traditional participant"; } }
		public IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			yield break;
		}

		public void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			addRow("Name", Name);
			addRow("Number", Number);
			addRow("CallType", SourceType);
			addRow("Direction", Direction);
			addRow("StartTime", Start);
		}

		public IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield return new ConsoleCommand("Hangup", "Ends the call", () => Hangup());
			yield return new GenericConsoleCommand<string>("SendDTMF", "SendDTMF x", s => SendDtmf(s));
		}

		#endregion
	}
}
