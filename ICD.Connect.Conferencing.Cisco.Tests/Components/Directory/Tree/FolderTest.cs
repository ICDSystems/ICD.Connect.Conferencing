using System;
using System.Collections.Generic;
using NUnit.Framework;
using ICD.Connect.Conferencing.Cisco.Components.Directory.Tree;

namespace ICD.Connect.Conferencing.Cisco.Tests.Components.Directory.Tree
{
	[TestFixture]
	public sealed class FolderTest
	{
		[Test]
		public void PhonebookTypeTest()
		{
			Folder folder = new Folder("Test", "Id");
			Assert.AreEqual(ePhonebookType.Corporate, folder.PhonebookType);

			folder = new Folder("Test2", "localId");
			Assert.AreEqual(ePhonebookType.Local, folder.PhonebookType);
		}

		[Test]
		public void AddFolderTest()
		{
			Folder parent = new Folder("Parent", "ParentId");

			Folder notPhonebook = new Folder("NotPhonebook", "localNotPhonebookId");
			Assert.IsFalse(parent.AddFolder(notPhonebook));

			Folder child = new Folder("Child", "ChildId");
			Assert.IsTrue(parent.AddFolder(child));
			Assert.IsFalse(parent.AddFolder(child));
		}

		[Test]
		public void AddContactTest()
		{
			Folder parent = new Folder("Parent", "ParentId");

			CiscoContact notPhonebook = new CiscoContact("NotPhonebook", "localNotPhonebookId", "ParentId", new CiscoContactMethod[0]);
			Assert.IsFalse(parent.AddContact(notPhonebook));

			CiscoContact child = new CiscoContact("Child", "ChildId", "ParentId", new CiscoContactMethod[0]);
			Assert.IsTrue(parent.AddContact(child));
			Assert.IsFalse(parent.AddContact(child));
		}

		[Test]
		public void ContactAddedFeedbackTest()
		{
			List<EventArgs> results = new List<EventArgs>();

			Folder parent = new Folder("Parent", "ParentId");

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

			Folder parent = new Folder("Parent", "ParentId");

			parent.OnContentsChanged += (sender, args) => results.Add(args);

			Folder child = new Folder("Child", "ChildId");
			parent.AddFolder(child);
			parent.AddFolder(child);

			Assert.AreEqual(1, results.Count);
		}
	}
}
