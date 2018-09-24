using System;
using ICD.Connect.Conferencing.Contacts;
using ICD.Connect.Conferencing.Controls.Directory;
using ICD.Connect.Conferencing.Directory.Tree;

namespace ICD.Connect.Conferencing.Mock
{
	public sealed class MockDirectoryControl : AbstractDirectoryControl<IMockConferencingDevice>
	{

		public override event EventHandler OnCleared;

		public MockDirectoryControl(IMockConferencingDevice parent, int id)
			: base(parent, id)
		{
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);
		}

		public override IDirectoryFolder GetRoot()
		{
			return new DirectoryFolder("MockFolder");
		}

		public override void Clear()
		{
			throw new NotImplementedException();
		}

		public override void PopulateFolder(IDirectoryFolder folder)
		{
			folder.AddContact(new Contact("MockPerson", new IContactMethod[]{ new ContactMethod("555-555-5555") }));
		}
	}
}
