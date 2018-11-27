using System;
using ICD.Connect.Conferencing.Contacts;
using ICD.Connect.Conferencing.Controls.Directory;
using ICD.Connect.Conferencing.Directory.Tree;

namespace ICD.Connect.Conferencing.Mock
{
	public sealed class MockDirectoryControl : AbstractDirectoryControl<IMockConferencingDevice>
	{
		public override event EventHandler OnCleared;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public MockDirectoryControl(IMockConferencingDevice parent, int id)
			: base(parent, id)
		{
		}

		public override IDirectoryFolder GetRoot()
		{
			return new DirectoryFolder("MockFolder");
		}

		public override void Clear()
		{
		}

		public override void PopulateFolder(IDirectoryFolder folder)
		{
			folder.AddContact(new Contact("MockPerson", new IContactMethod[]{ new ContactMethod("555-555-5555") }));
		}
	}
}
