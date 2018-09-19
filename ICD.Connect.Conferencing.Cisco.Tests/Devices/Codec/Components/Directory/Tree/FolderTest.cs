using System;
using System.Collections.Generic;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Directory.Tree;
using NUnit.Framework;

namespace ICD.Connect.Conferencing.Cisco.Tests.Devices.Codec.Components.Directory.Tree
{
	[TestFixture]
	public sealed class FolderTest
	{
		[Test]
		public void PhonebookTypeTest()
		{
			CiscoFolder folder = new CiscoFolder
			{
				FolderId = "Id"
			};
			Assert.AreEqual(ePhonebookType.Corporate, folder.PhonebookType);

			folder = new CiscoFolder
			{
				FolderId = "localId"
			};
			Assert.AreEqual(ePhonebookType.Local, folder.PhonebookType);
		}

		[Test]
		public void AddFolderTest()
		{
			CiscoFolder parent = new CiscoFolder
			{
				FolderId = "ParentId"
			};
			parent.SetName("parent");

			CiscoFolder child = new CiscoFolder
			{
				FolderId = "ChildId"
			};
			child.SetName("child");

			Assert.IsTrue(parent.AddFolder(child));
			Assert.IsFalse(parent.AddFolder(child));
		}

		[Test]
		public void AddContactTest()
		{
			CiscoFolder parent = new CiscoFolder
			{
				FolderId = "ParentId"
			};
			parent.SetName("parent");

			CiscoContact child = new CiscoContact(){Name = "child"};

			Assert.IsTrue(parent.AddContact(child));
			Assert.IsFalse(parent.AddContact(child));
		}

		[Test]
		public void ContactAddedFeedbackTest()
		{
			List<EventArgs> results = new List<EventArgs>();

			CiscoFolder parent = new CiscoFolder
			{
				FolderId = "ParentId"
			};
			parent.SetName("parent");

			parent.OnContentsChanged += (sender, args) => results.Add(args);

			CiscoContact child = new CiscoContact(){Name = "child"};
			parent.AddContact(child);
			parent.AddContact(child);

			Assert.AreEqual(1, results.Count);
		}

		[Test]
		public void FolderAddedFeedbackTest()
		{
			List<EventArgs> results = new List<EventArgs>();

			CiscoFolder parent = new CiscoFolder
			{
				FolderId = "ParentId"
			};
			parent.SetName("parent");

			parent.OnContentsChanged += (sender, args) => results.Add(args);

			CiscoFolder child = new CiscoFolder
			{
				FolderId = "ChildId"
			};
			child.SetName("child");

			parent.AddFolder(child);
			parent.AddFolder(child);

			Assert.AreEqual(1, results.Count);
		}
	}
}
