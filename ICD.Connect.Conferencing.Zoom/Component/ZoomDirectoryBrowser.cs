using System;
using System.Linq;
using ICD.Connect.Conferencing.Directory;
using ICD.Connect.Conferencing.Directory.Tree;
using ICD.Connect.Conferencing.Zoom.Responses;

namespace ICD.Connect.Conferencing.Zoom.Component
{
	public class ZoomDirectoryBrowser : AbstractDirectoryBrowser<IFolder, ZoomContact>
	{
		private const int RESULT_SIZE = 1000;

		private readonly ZoomRoom m_ZoomRoom;

		private IFolder m_Folder;

		public ZoomDirectoryBrowser(ZoomRoom zoomRoom)
		{
			m_ZoomRoom = zoomRoom;
			Subscribe(m_ZoomRoom);
		}

		protected override void PopulateFolder(IFolder parent)
		{
			m_ZoomRoom.SendCommand("zCommand Phonebook List offset: 0 limit: {0}", RESULT_SIZE);
			m_Folder = parent;
		}

		private void Subscribe(ZoomRoom zoomRoom)
		{
			zoomRoom.RegisterResponseCallback<PhonebookContactUpdatedResponse>(ContactUpdatedCallback);
			zoomRoom.RegisterResponseCallback<PhonebookListCommandResponse>(PhonebookListCallback);
		}

		private void ContactUpdatedCallback(ZoomRoom zoomRoom, PhonebookContactUpdatedResponse response)
		{
			throw new NotImplementedException();
		}

		private void PhonebookListCallback(ZoomRoom zoomRoom, PhonebookListCommandResponse response)
		{
			var result = response.PhonebookListResult;
			if (result.Contacts.Count() >= result.Limit)
			{
				m_ZoomRoom.SendCommand(
					"zCommand Phonebook List offset: {0} limit: {1}",
					result.Offset + result.Contacts.Count(),
					RESULT_SIZE);
			}

			m_Folder.AddContacts(result.Contacts);
		}
	}
}