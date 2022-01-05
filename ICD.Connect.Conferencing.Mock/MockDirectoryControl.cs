using System;
using System.Collections.Generic;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Contacts;
using ICD.Connect.Conferencing.Controls.Directory;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.Directory.Tree;

namespace ICD.Connect.Conferencing.Mock
{
	public sealed class MockDirectoryControl : AbstractDirectoryControl<IMockConferencingDevice>
	{
		public override event EventHandler OnCleared;

		private IDirectoryFolder Root { get; set; }

		public MockDirectoryControl(IMockConferencingDevice parent, int id)
			: base(parent, id)
		{
		}

		public override IDirectoryFolder GetRoot()
		{
			return Root = Root ?? new DirectoryFolder("MockFolder");
		}

		public override void Clear()
		{
			if (Root != null)
				Root.ClearRecursive();
			OnCleared.Raise(this);
		}

		public override void PopulateFolder(IDirectoryFolder folder)
		{
			Contact contact = new Contact
			{
				Name = "MockPerson",
				DialContexts = new List<IDialContext>
				{
					new DialContext {Protocol = eDialProtocol.Pstn, DialString = "555-555-5555"}
				}
			};

			folder.AddContact(contact);
		}
	}
}
