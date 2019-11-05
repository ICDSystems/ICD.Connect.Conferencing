using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Zoom.Responses;

namespace ICD.Connect.Conferencing.Zoom.Components.System
{
	public sealed class SystemComponent : AbstractZoomRoomComponent
	{
		private SystemInfo m_SystemInfo;

		public SystemInfo SystemInfo { get { return m_SystemInfo; } }

		public SystemComponent(ZoomRoom zoomRoom) : base(zoomRoom)
		{
			Subscribe(zoomRoom);
		}

		private void Subscribe(ZoomRoom zoomRoom)
		{
			zoomRoom.RegisterResponseCallback<SystemUnitResponse>(SystemInfoCallback);
		}

		private void SystemInfoCallback(ZoomRoom zoomRoom, SystemUnitResponse response)
		{
			m_SystemInfo = response.SystemInfo;
		}

		protected override void Initialize()
		{
			base.Initialize();

			Parent.SendCommand("zConfiguration Client deviceSystem: \"Krang Zoom Room Controller\"");
			Parent.SendCommand("zConfiguration Client appVersion: {0}", GetType().GetAssembly().GetName().Version);
			Parent.SendCommand("zStatus SystemUnit");
		}

		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			if (m_SystemInfo == null)
				return;

			addRow("Email", m_SystemInfo.Email);
			addRow("Meeting Number", m_SystemInfo.MeetingNumber);
			addRow("Login Type", m_SystemInfo.LoginType);
			addRow("Platform", m_SystemInfo.Platform);
			addRow("Room Version", m_SystemInfo.RoomVersion);

			if (m_SystemInfo.RoomInfo == null)
				return;

			addRow("Room Name", m_SystemInfo.RoomInfo.RoomName);
			addRow("Account Email", m_SystemInfo.RoomInfo.AccountEmail);
			addRow("IsAutoAnswerEnabled", m_SystemInfo.RoomInfo.IsAutoAnswerEnabled);
			addRow("IsAutoAnswerSelected", m_SystemInfo.RoomInfo.IsAutoAnswerSelected);
		}
	}
}