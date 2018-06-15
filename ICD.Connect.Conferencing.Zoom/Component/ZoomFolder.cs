using ICD.Connect.Conferencing.Contacts;
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

		protected override bool AddContact(IContact contact, bool raise)
		{
			
			return base.AddContact(contact, raise);
		}
	}
}