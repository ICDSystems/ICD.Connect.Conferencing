using System;
using System.Collections.Generic;
using NUnit.Framework;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Directory.Tree;

namespace ICD.Connect.Conferencing.Cisco.Tests.Components.Directory.Tree
{
	[TestFixture]
	public sealed class FolderTest
	{
		[Test]
		public void PhonebookTypeTest()
		{
			CiscoFolder folder = new CiscoFolder("Test", "Id");
			Assert.AreEqual(ePhonebookType.Corporate, folder.PhonebookType);

			folder = new CiscoFolder("Test2", "localId");
			Assert.AreEqual(ePhonebookType.Local, folder.PhonebookType);
		}

		[Test]
		public void AddFolderTest()
		{
			CiscoFolder parent = new CiscoFolder("Parent", "ParentId");

			CiscoFolder child = new CiscoFolder("Child", "ChildId");
			Assert.IsTrue(parent.AddFolder(child));
			Assert.IsFalse(parent.AddFolder(child));
		}

		[Test]
		public void AddContactTest()
		{
			CiscoFolder parent = new CiscoFolder("Parent", "ParentId");

			CiscoContact child = new CiscoContact("Child", "ChildId", "ParentId", new CiscoContactMethod[0]);
			Assert.IsTrue(parent.AddContact(child));
			Assert.IsFalse(parent.AddContact(child));
		}

		[Test]
		public void ContactAddedFeedbackTest()
		{
			List<EventArgs> results = new List<EventArgs>();

			CiscoFolder parent = new CiscoFolder("Parent", "ParentId");

			parent.OnContentsChanged += (sender, args) => results.Add(args);

			CiscoContact child = new CiscoContact("Child", "ChildId", "ParentId", new CiscoContactMethod[0]);
			parent.AddContact(child);
			parent.AddContact(child);

			Assert.AreEqual(1, results.Count);
		}

		[Test]
		public void FolderAddedFeedbackTest()
		{
			List<EventArgs> results = new List<EventArgs>();

			CiscoFolder parent = new CiscoFolder("Parent", "ParentId");

			parent.OnContentsChanged += (sender, args) => results.Add(args);

			CiscoFolder child = new CiscoFolder("Child", "ChildId");
			parent.AddFolder(child);
			parent.AddFolder(child);

			Assert.AreEqual(1, results.Count);
		}
	}
}
