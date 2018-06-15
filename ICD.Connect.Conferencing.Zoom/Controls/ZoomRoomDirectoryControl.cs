using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using ICD.Connect.Conferencing.Controls.Directory;
using ICD.Connect.Conferencing.Directory.Tree;
using ICD.Connect.Conferencing.Zoom.Component;
using ICD.Connect.Conferencing.Zoom.Responses;

namespace ICD.Connect.Conferencing.Zoom.Controls
{
	public class ZoomRoomDirectoryControl : AbstractDirectoryControl<ZoomRoom>
	{
		private ZoomFolder m_Folder;

		public ZoomRoomDirectoryControl(ZoomRoom parent, int id) : base(parent, id)
		{
			m_Folder = new ZoomFolder();
			Subscribe(parent);
		}

		public override event EventHandler OnCleared;
		public override IDirectoryFolder GetRoot()
		{
			return m_Folder;
		}

		public override void Clear()
		{
			m_Folder.Clear();
		}

		public override void PopulateFolder(IDirectoryFolder folder)
		{
			Parent.SendCommand("zCommand Phonebook List");
		}

		#region Private Methods

		private void Subscribe(ZoomRoom zoomRoom)
		{
			zoomRoom.RegisterResponseCallback<PhonebookListCommandResponse>(ListPhonebookCallback);
		}

		private void ListPhonebookCallback(ZoomRoom zoomRoom, PhonebookListCommandResponse response)
		{
			m_Folder.AddContacts(response.PhonebookListResult.Contacts);
		}

		#endregion
	}
}