using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Directory;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Directory.Tree;
using ICD.Connect.Conferencing.Directory.Tree;
using NUnit.Framework;

namespace ICD.Connect.Conferencing.Cisco.Tests.Devices.Codec.Components.Directory
{
	[TestFixture]
	public sealed class DirectoryComponentTest : AbstractCiscoComponentTest
	{
		#region XML

		private const string EXAMPLE_XML =
			"<XmlDoc resultId=\"{0}\">"
			+ "<PhonebookSearchResult item=\"1\" status=\"OK\">"
			+ "<ResultInfo item=\"1\">"
			+ "<Offset item=\"1\">0</Offset>"
			+ "<Limit item=\"1\">50</Limit>"
			+ "<TotalRows item=\"1\">21</TotalRows>"
			+ "</ResultInfo>"
			+ "<Folder item=\"1\" localId=\"localGroupId-3\">"
			+ "<Name item=\"1\">CA</Name>"
			+ "<FolderId item=\"1\">localGroupId-3</FolderId>"
			+ "<ParentFolderId item=\"1\">localGroupId-2</ParentFolderId>"
			+ "</Folder>"
			+ "<Folder item=\"2\" localId=\"localGroupId-19\">"
			+ "<Name item=\"1\">Exton, PA</Name>"
			+ "<FolderId item=\"1\">localGroupId-19</FolderId>"
			+ "<ParentFolderId item=\"1\">localGroupId-4</ParentFolderId>"
			+ "</Folder>"
			+ "<Folder item=\"3\" localId=\"localGroupId-1\">"
			+ "<Name item=\"1\">International</Name>"
			+ "<FolderId item=\"1\">localGroupId-1</FolderId>"
			+ "</Folder>"
			+ "<Folder item=\"4\" localId=\"localGroupId-18\">"
			+ "<Name item=\"1\">Las Vegas, NV</Name>"
			+ "<FolderId item=\"1\">localGroupId-18</FolderId>"
			+ "<ParentFolderId item=\"1\">localGroupId-6</ParentFolderId>"
			+ "</Folder>"
			+ "<Folder item=\"5\" localId=\"localGroupId-5\">"
			+ "<Name item=\"1\">NC</Name>"
			+ "<FolderId item=\"1\">localGroupId-5</FolderId>"
			+ "<ParentFolderId item=\"1\">localGroupId-2</ParentFolderId>"
			+ "</Folder>"
			+ "<Folder item=\"6\" localId=\"localGroupId-6\">"
			+ "<Name item=\"1\">NV</Name>"
			+ "<FolderId item=\"1\">localGroupId-6</FolderId>"
			+ "<ParentFolderId item=\"1\">localGroupId-2</ParentFolderId>"
			+ "</Folder>"
			+ "<Folder item=\"7\" localId=\"localGroupId-4\">"
			+ "<Name item=\"1\">PA</Name>"
			+ "<FolderId item=\"1\">localGroupId-4</FolderId>"
			+ "<ParentFolderId item=\"1\">localGroupId-2</ParentFolderId>"
			+ "</Folder>"
			+ "<Folder item=\"8\" localId=\"localGroupId-17\">"
			+ "<Name item=\"1\">Raleigh, NC</Name>"
			+ "<FolderId item=\"1\">localGroupId-17</FolderId>"
			+ "<ParentFolderId item=\"1\">localGroupId-5</ParentFolderId>"
			+ "</Folder>"
			+ "<Folder item=\"9\" localId=\"localGroupId-20\">"
			+ "<Name item=\"1\">San Francisco, CA</Name>"
			+ "<FolderId item=\"1\">localGroupId-20</FolderId>"
			+ "<ParentFolderId item=\"1\">localGroupId-3</ParentFolderId>"
			+ "</Folder>"
			+ "<Folder item=\"10\" localId=\"localGroupId-16\">"
			+ "<Name item=\"1\">Tustin, CA</Name>"
			+ "<FolderId item=\"1\">localGroupId-16</FolderId>"
			+ "<ParentFolderId item=\"1\">localGroupId-3</ParentFolderId>"
			+ "</Folder>"
			+ "<Folder item=\"11\" localId=\"localGroupId-2\">"
			+ "<Name item=\"1\">USA</Name>"
			+ "<FolderId item=\"1\">localGroupId-2</FolderId>"
			+ "</Folder>"
			+ "<Contact item=\"1\">"
			+ "<Name item=\"1\">Bradd Fisher</Name>"
			+ "<ContactId item=\"1\">localContactId-10</ContactId>"
			+ "<FolderId item=\"1\">localGroupId-18</FolderId>"
			+ "<Title item=\"1\">President</Title>"
			+ "<ContactMethod item=\"1\">"
			+ "<ContactMethodId item=\"1\">1</ContactMethodId>"
			+ "<Number item=\"1\">111</Number>"
			+ "<CallType item=\"1\">Video</CallType>"
			+ "</ContactMethod>"
			+ "</Contact>"
			+ "<Contact item=\"2\">"
			+ "<Name item=\"1\">Brett Fisher</Name>"
			+ "<ContactId item=\"1\">localContactId-11</ContactId>"
			+ "<FolderId item=\"1\">localGroupId-19</FolderId>"
			+ "<Title item=\"1\">Vice President</Title>"
			+ "<ContactMethod item=\"1\">"
			+ "<ContactMethodId item=\"1\">1</ContactMethodId>"
			+ "<Number item=\"1\">112</Number>"
			+ "<CallType item=\"1\">Video</CallType>"
			+ "</ContactMethod>"
			+ "</Contact>"
			+ "<Contact item=\"3\">"
			+ "<Name item=\"1\">Brett Heroux</Name>"
			+ "<ContactId item=\"1\">localContactId-15</ContactId>"
			+ "<FolderId item=\"1\">localGroupId-17</FolderId>"
			+ "<Title item=\"1\">Programmer</Title>"
			+ "<ContactMethod item=\"1\">"
			+ "<ContactMethodId item=\"1\">1</ContactMethodId>"
			+ "<Number item=\"1\">116</Number>"
			+ "<CallType item=\"1\">Video</CallType>"
			+ "</ContactMethod>"
			+ "</Contact>"
			+ "<Contact item=\"4\">"
			+ "<Name item=\"1\">Chris Van</Name>"
			+ "<ContactId item=\"1\">localContactId-12</ContactId>"
			+ "<FolderId item=\"1\">localGroupId-19</FolderId>"
			+ "<Title item=\"1\">Project Manager</Title>"
			+ "<ContactMethod item=\"1\">"
			+ "<ContactMethodId item=\"1\">1</ContactMethodId>"
			+ "<Number item=\"1\">125</Number>"
			+ "<CallType item=\"1\">Video</CallType>"
			+ "</ContactMethod>"
			+ "</Contact>"
			+ "<Contact item=\"5\">"
			+ "<Name item=\"1\">Corey Geiser</Name>"
			+ "<ContactId item=\"1\">localContactId-13</ContactId>"
			+ "<FolderId item=\"1\">localGroupId-19</FolderId>"
			+ "<Title item=\"1\">Graphics</Title>"
			+ "<ContactMethod item=\"1\">"
			+ "<ContactMethodId item=\"1\">1</ContactMethodId>"
			+ "<Number item=\"1\">115</Number>"
			+ "<CallType item=\"1\">Video</CallType>"
			+ "</ContactMethod>"
			+ "</Contact>"
			+ "<Contact item=\"6\">"
			+ "<Name item=\"1\">Drew Tingen</Name>"
			+ "<ContactId item=\"1\">localContactId-14</ContactId>"
			+ "<FolderId item=\"1\">localGroupId-17</FolderId>"
			+ "<Title item=\"1\">Programmer</Title>"
			+ "<ContactMethod item=\"1\">"
			+ "<ContactMethodId item=\"1\">1</ContactMethodId>"
			+ "<Number item=\"1\">119</Number>"
			+ "<CallType item=\"1\">Video</CallType>"
			+ "</ContactMethod>"
			+ "</Contact>"
			+ "<Contact item=\"7\">"
			+ "<Name item=\"1\">Holly Kohlmann</Name>"
			+ "<ContactId item=\"1\">localContactId-8</ContactId>"
			+ "<FolderId item=\"1\">localGroupId-16</FolderId>"
			+ "<Title item=\"1\">Accountant</Title>"
			+ "<ContactMethod item=\"1\">"
			+ "<ContactMethodId item=\"1\">1</ContactMethodId>"
			+ "<Number item=\"1\">131</Number>"
			+ "<CallType item=\"1\">Video</CallType>"
			+ "</ContactMethod>"
			+ "</Contact>"
			+ "<Contact item=\"8\">"
			+ "<Name item=\"1\">Jeff Wojo</Name>"
			+ "<ContactId item=\"1\">localContactId-9</ContactId>"
			+ "<FolderId item=\"1\">localGroupId-16</FolderId>"
			+ "<Title item=\"1\">Technician</Title>"
			+ "<ContactMethod item=\"1\">"
			+ "<ContactMethodId item=\"1\">1</ContactMethodId>"
			+ "<Number item=\"1\">203</Number>"
			+ "<CallType item=\"1\">Video</CallType>"
			+ "</ContactMethod>"
			+ "</Contact>"
			+ "<Contact item=\"9\">"
			+ "<Name item=\"1\">Mark Kohlmann</Name>"
			+ "<ContactId item=\"1\">localContactId-7</ContactId>"
			+ "<FolderId item=\"1\">localGroupId-16</FolderId>"
			+ "<Title item=\"1\">Programmer</Title>"
			+ "<ContactMethod item=\"1\">"
			+ "<ContactMethodId item=\"1\">1</ContactMethodId>"
			+ "<Number item=\"1\">120</Number>"
			+ "<CallType item=\"1\">Video</CallType>"
			+ "</ContactMethod>"
			+ "</Contact>"
			+ "<Contact item=\"10\">"
			+ "<Name item=\"1\">Root Contact</Name>"
			+ "<ContactId item=\"1\">localContactId-21</ContactId>"
			+ "<Title item=\"1\"></Title>"
			+ "<ContactMethod item=\"1\">"
			+ "<ContactMethodId item=\"1\">1</ContactMethodId>"
			+ "<Number item=\"1\">115</Number>"
			+ "<CallType item=\"1\">Video</CallType>"
			+ "</ContactMethod>"
			+ "</Contact>"
			+ "</PhonebookSearchResult>"
			+ "</XmlDoc>";

		#endregion

		[Test]
		public void DirectoryBuildingTest()
		{
			DirectoryComponent component = Codec.Components.GetComponent<DirectoryComponent>();

			string rX = string.Format(EXAMPLE_XML, component.GetRoot(ePhonebookType.Local).FolderSearchId);

			Port.Receive(rX);

			ThreadingUtils.Wait(() => component.GetRoot(ePhonebookType.Local).ChildCount == 21, 5 * 1000);

			Assert.AreEqual(11, component.GetRoot(ePhonebookType.Local).GetFolders().Length);
			Assert.AreEqual(10, component.GetRoot(ePhonebookType.Local).GetContacts().Length);
		}

		[Test]
		public void FolderSearchResultFeedbackTest()
		{
			DirectoryComponent component = Codec.Components.GetComponent<DirectoryComponent>();
			List<IDirectoryFolder> results = new List<IDirectoryFolder>();

			component.OnResultParsed += (id, folders, contacts) => results.AddRange(folders);

			Port.Receive(EXAMPLE_XML);

			ThreadingUtils.Wait(() => component.GetRoot(ePhonebookType.Local).ChildCount == 21, 5 * 1000);

			Assert.AreEqual(11, results.Count);
		}

		[Test]
		public void ContactSearchResultFeedbackTest()
		{
			DirectoryComponent component = Codec.Components.GetComponent<DirectoryComponent>();
			List<CiscoContact> results = new List<CiscoContact>();

			component.OnResultParsed += (id, folders, contacts) => results.AddRange(contacts);

			Port.Receive(EXAMPLE_XML);

			ThreadingUtils.Wait(() => component.GetRoot(ePhonebookType.Local).ChildCount == 21, 5 * 1000);

			Assert.AreEqual(10, results.Count);
		}
	}
}
