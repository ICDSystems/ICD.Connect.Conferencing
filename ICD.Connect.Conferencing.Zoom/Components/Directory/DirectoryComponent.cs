using System;
using System.Collections.Generic;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Zoom.Responses;

namespace ICD.Connect.Conferencing.Zoom.Components.Directory
{
	public class DirectoryComponent : AbstractZoomRoomComponent
	{
		private readonly ZoomFolder m_RootFolder;

		public DirectoryComponent(ZoomRoom parent) : base(parent)
		{
			m_RootFolder = new ZoomFolder("Root");
			Subscribe(parent);
		}

		protected override void DisposeFinal()
		{
			Unsubscribe(Parent);
			base.DisposeFinal();
		}

		#region Methods

		public ZoomFolder GetRoot()
		{
			return m_RootFolder;
		}

		public void Populate()
		{
			Parent.SendCommand("zCommand Phonebook List offset: 0 limit: 9999999");
		}

		#endregion

		#region Zoom Room Callbacks

		private void Subscribe(ZoomRoom zoomRoom)
		{
			zoomRoom.RegisterResponseCallback<PhonebookListCommandResponse>(PhonebookListCallback);
		}

		private void Unsubscribe(ZoomRoom zoomRoom)
		{
			zoomRoom.UnregisterResponseCallback<PhonebookListCommandResponse>(PhonebookListCallback);
		}

		private void PhonebookListCallback(ZoomRoom zoomRoom, PhonebookListCommandResponse response)
		{
			foreach (var contact in response.PhonebookListResult.Contacts)
			{
				if (contact.IsZoomRoom)
				{
					var roomsFolder = m_RootFolder.GetFolder("Rooms");
					if (roomsFolder == null)
					{
						roomsFolder = new ZoomFolder("Rooms");
						m_RootFolder.AddFolder(roomsFolder);
					}
					roomsFolder.AddContact(contact);
				}
				else
				{
					try
					{
						string lastNameLetter = contact.LastName.ToUpper()[0].ToString();
						var letterFolder = m_RootFolder.GetFolder(lastNameLetter);
						if (letterFolder == null)
						{
							letterFolder = new ZoomFolder(lastNameLetter);
							m_RootFolder.AddFolder(letterFolder);
						}
						letterFolder.AddContact(contact);
					}
					catch (Exception)
					{
						//skip contact
					}
				}
			}
		}

		#endregion

		#region Console

		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Contacts", m_RootFolder.ContactCount);
		}

		public override string ConsoleHelp
		{
			get { return "Zoom component for directory operations"; }
		}

		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield return new ConsoleCommand("PopulateDirectory", "Populates the directory with Zoom contacts", () => Populate());
		}

		#endregion
	}
}