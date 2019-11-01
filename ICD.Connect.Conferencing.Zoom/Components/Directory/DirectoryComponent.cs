using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Contacts;
using ICD.Connect.Conferencing.Directory.Tree;
using ICD.Connect.Conferencing.Zoom.Responses;

namespace ICD.Connect.Conferencing.Zoom.Components.Directory
{
	public sealed class DirectoryComponent : AbstractZoomRoomComponent
	{
		// Make this to true to have zoom rooms and contacts sorted into folders
		private const bool USE_FOLDERS = false;

		private const string ROOMS_FOLDER = "Rooms";
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
			if (Initialized)
				Parent.SendCommand("zCommand Phonebook List offset: 0 limit: 1000");
		}

		#endregion

		#region Private Methods

		private static string GetFolderNameForContact(ZoomContact contact)
		{
			if (contact == null)
				throw new ArgumentNullException("contact");

			return contact.IsZoomRoom ? ROOMS_FOLDER : contact.LastName.ToUpper()[0].ToString();
		}

		private void AddOrUpdateContact(ZoomContact contact)
		{
			if (contact == null)
				throw new ArgumentNullException("contact");

			AddOrUpdateContacts(new[] {contact});
		}

		private void AddOrUpdateContacts(IEnumerable<ZoomContact> zoomContacts)
		{
			if (zoomContacts == null)
				throw new ArgumentNullException("zoomContacts");

			if (!USE_FOLDERS)
			{
				AddOrUpdateContacts(m_RootFolder, zoomContacts);
				return;
			}

			foreach (var groupByFolder in zoomContacts.GroupBy(c => GetFolder(c)))
				AddOrUpdateContacts(groupByFolder.Key, groupByFolder);
		}

		private static void AddOrUpdateContacts(IDirectoryFolder folder, IEnumerable<ZoomContact> zoomContacts)
		{
			if (zoomContacts == null)
				throw new ArgumentNullException("zoomContacts");

			List<IContact> toAdd = new List<IContact>();

			foreach (ZoomContact contact in zoomContacts)
			{
				// fix for SIP/H.323 devices showing up with no first and last name
				if (string.IsNullOrEmpty(contact.FirstName) && string.IsNullOrEmpty(contact.LastName))
				{
					if (string.IsNullOrEmpty(contact.ScreenName))
						continue;

					contact.LastName = contact.ScreenName;
				}

				ZoomContact closure = contact;
				var existingContact = folder.GetContacts()
				                            .OfType<ZoomContact>()
				                            .SingleOrDefault(c => c.JoinId == closure.JoinId);

				if (existingContact == null)
					toAdd.Add(contact);
				else
					existingContact.Update(contact);
			}

			// Perf - Add as many contacts as possible in a single pass
			folder.AddContacts(toAdd);
		}

		[NotNull]
		private IDirectoryFolder GetFolder(ZoomContact contact)
		{
			var folderName = GetFolderNameForContact(contact);
			var folder = m_RootFolder.GetFolder(folderName);
			if (folder != null)
				return folder;

			folder = new ZoomFolder(folderName);
			m_RootFolder.AddFolder(folder);

			return folder;
		}

		#endregion

		#region Zoom Room Callbacks

		private void Subscribe(ZoomRoom zoomRoom)
		{
			zoomRoom.RegisterResponseCallback<PhonebookListCommandResponse>(PhonebookListCallback);
			zoomRoom.RegisterResponseCallback<PhonebookContactUpdatedResponse>(ContactUpdatedCallback);
		}

		private void Unsubscribe(ZoomRoom zoomRoom)
		{
			zoomRoom.UnregisterResponseCallback<PhonebookListCommandResponse>(PhonebookListCallback);
			zoomRoom.UnregisterResponseCallback<PhonebookContactUpdatedResponse>(ContactUpdatedCallback);
		}

		protected override void Initialize()
		{
			base.Initialize();

			Populate();
		}

		private void PhonebookListCallback(ZoomRoom zoomRoom, PhonebookListCommandResponse response)
		{
			PhonebookListResult result = response.PhonebookListResult;
			if (result == null)
				return;

			ZoomContact[] contacts = result.Contacts;
			if (contacts == null)
				return;

			AddOrUpdateContacts(contacts.Where(c => c != null));

			if (contacts.Length >= result.Limit)
				Parent.SendCommand("zCommand Phonebook List offset: {0} limit: 1000", result.Offset + result.Limit);
		}

		private void ContactUpdatedCallback(ZoomRoom zoomRoom, PhonebookContactUpdatedResponse response)
		{
			var contact = response.Data.Contact;
			if (contact == null)
				return;

			AddOrUpdateContact(contact);
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