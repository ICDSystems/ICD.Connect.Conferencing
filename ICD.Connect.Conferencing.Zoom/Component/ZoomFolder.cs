using ICD.Connect.Conferencing.Directory.Tree;
using ICD.Connect.Conferencing.Zoom.Responses;

namespace ICD.Connect.Conferencing.Zoom.Component
{
	public class ZoomFolder : AbstractDirectoryFolder
	{
		public override string Name
		{
			get { return "Zoom Contacts"; }
		}

		protected override bool AddFolder(IDirectoryFolder folder, bool raise)
		{
			return false;
		}

		public void AddOrUpdateContact(ZoomContact contact)
		{
			//int index = m_CachedContacts.FindIndex(c => c is ZoomContact && (c as ZoomContact).JoinId == contact.JoinId );
			//if (index < 0)
			//    AddContact(contact, true);
			//else
			//{
			//    m_CachedContacts[index] = contact;
			//}
		}
	}
}