using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;
using ICD.Connect.Conferencing.Zoom.Responses;
using ICD.Connect.Routing.Endpoints.Sources;

namespace ICD.Connect.Conferencing.Zoom.Components.Call
{
	public class ZoomParticipant : IWebParticipant
	{
		/// <summary>
		/// Raised when the participant's status changes.
		/// </summary>
		public event EventHandler<ParticipantStatusEventArgs> OnStatusChanged;

		/// <summary>
		/// Raised when the participant's source type changes.
		/// </summary>
		public event EventHandler<ParticipantTypeEventArgs> OnSourceTypeChanged;

		/// <summary>
		/// Raised when the participant's name changes.
		/// </summary>
		public event EventHandler<StringEventArgs> OnNameChanged;

		/// <summary>
		/// Raised when the participant's mute state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnIsMutedChanged;

		private string m_Name;
		private eCallType m_SourceType;
		private eParticipantStatus m_Status;
		private bool m_IsMuted;
		private ZoomRoom m_ZoomRoom;

		#region Properties

		public string Name
		{
			get { return m_Name; }
			private set
			{
				if (m_Name == value)
					return;

				m_Name = value;
				OnNameChanged.Raise(this, new StringEventArgs(m_Name));
			}
		}

		public eCallType SourceType
		{
			get { return m_SourceType; }
			private set
			{
				if (m_SourceType == value)
					return;
				m_SourceType = value;
				OnSourceTypeChanged.Raise(this, new ParticipantTypeEventArgs(m_SourceType));
			}
		}

		public eParticipantStatus Status
		{
			get { return m_Status; }
			private set
			{
				if (m_Status == value)
					return;

				m_Status = value;
				OnStatusChanged.Raise(this, new ParticipantStatusEventArgs(m_Status));
			}
		}

		public bool IsMuted
		{
			get { return m_IsMuted; }
			private set
			{
				if (m_IsMuted == value)
					return;

				m_IsMuted = value;
				OnIsMutedChanged.Raise(this, new BoolEventArgs(m_IsMuted));
			}
		}

		public string UserId { get; private set; }
		public string AvatarUrl { get; private set; }
		public DateTime? Start { get; private set; }
		public DateTime? End { get; private set; }

		#endregion

		public ZoomParticipant(ZoomRoom zoomRoom, ParticipantInfo info)
		{
			m_ZoomRoom = zoomRoom;
			UserId = info.UserId;
			Start = IcdEnvironment.GetLocalTime();
			Update(info);
		}

		#region Methods

		public void Update(ParticipantInfo info)
		{
			Name = info.UserName;
			Status = eParticipantStatus.Connected;
			SourceType = info.IsSendingVideo ? eCallType.Video : eCallType.Audio;
			IsMuted = info.AudioState == eAudioState.AUDIO_MUTED;
			AvatarUrl = info.AvatarUrl;
		}

		public void Kick()
		{
			m_ZoomRoom.SendCommand("zCommand Call Expel Id: {0}", UserId);
		}

		public void Mute(bool mute)
		{
			m_ZoomRoom.SendCommand("zCommand Call MuteParticipant mute: {0} Id: {1}", 
				mute ? "on" : "off", 
				UserId);
		}

		#endregion

		#region Console

		public string ConsoleName { get { return Name; } }

		public string ConsoleHelp { get { return "Zoom conference participant"; } }

		public IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			yield break;
		}

		public void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			addRow("Name", Name);
			addRow("UserId", UserId);
			addRow("Status", Status);
			addRow("SourceType", SourceType);
			addRow("Start", Start);
			addRow("End", End);
		}

		public IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield return new GenericConsoleCommand<bool>("Mute", "Usage: Mute <true/false>", (m) => Mute(m));
			yield return new ConsoleCommand("Kick", "Kick the participant", () => Kick());
		}
		#endregion
	}
}