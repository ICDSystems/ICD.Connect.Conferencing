using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Xml;
using ICD.Connect.API.Commands;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Directory.Tree;
using ICD.Connect.Conferencing.Contacts;
using ICD.Connect.Conferencing.Directory.Tree;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Directory
{
	public enum eContactType
	{
		[PublicAPI] Any,
		[PublicAPI] Folder,
		[PublicAPI] Contact
	}

	/// <summary>
	/// DirectoryComponent provides functionality for using the codec directory features.
	/// </summary>
	public sealed class DirectoryComponent : AbstractCiscoComponent
	{
		/// <summary>
		/// Callback for result parsing.
		/// </summary>
		/// <param name="resultId"></param>
		/// <param name="folders"></param>
		/// <param name="contacts"></param>
		public delegate void ResultParsedDelegate(string resultId, CiscoFolder[] folders, CiscoContact[] contacts);

		/// <summary>
		/// Called when the cache is cleared.
		/// </summary>
		public event EventHandler OnCleared;

		/// <summary>
		/// Called when a result is parsed.
		/// </summary>
		public event ResultParsedDelegate OnResultParsed;

		// Mapping folder/contact ids
		private readonly Dictionary<string, CiscoFolder> m_Folders;
		private readonly Dictionary<string, CiscoContact> m_Contacts;

		private readonly Dictionary<ePhonebookType, CiscoRootFolder> m_RootsCache;

		private readonly SafeCriticalSection m_FolderSection;
		private readonly SafeCriticalSection m_RootsSection;

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="codec"></param>
		public DirectoryComponent(CiscoCodecDevice codec)
			: base(codec)
		{
			m_RootsCache = new Dictionary<ePhonebookType, CiscoRootFolder>();
			m_Folders = new Dictionary<string, CiscoFolder>();
			m_Contacts = new Dictionary<string, CiscoContact>();

			m_FolderSection = new SafeCriticalSection();
			m_RootsSection = new SafeCriticalSection();

			Subscribe(Codec);

			if (Codec.Initialized)
				Initialize();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			OnCleared = null;
			OnResultParsed = null;

			base.Dispose(disposing);
		}

		/// <summary>
		/// Clears the cache.
		/// </summary>
		public void Clear()
		{
			m_FolderSection.Enter();

			try
			{
				foreach (CiscoRootFolder root in GetRoots())
					root.ClearRecursive();

				// Folders
				m_Folders.Clear();

				// Contacts
				m_Contacts.Clear();
			}
			finally
			{
				m_FolderSection.Leave();
			}

			OnCleared.Raise(this);
		}

		/// <summary>
		/// Gets the root for the configured phonebook type.
		/// </summary>
		/// <returns></returns>
		public CiscoRootFolder GetRoot()
		{
			return GetRoot(Codec.PhonebookType);
		}

		/// <summary>
		/// Gets the root for the given phonebook type.
		/// </summary>
		/// <param name="phonebookType"></param>
		/// <returns></returns>
		public CiscoRootFolder GetRoot(ePhonebookType phonebookType)
		{
			m_RootsSection.Enter();

			try
			{
				CiscoRootFolder root;
				if (!m_RootsCache.TryGetValue(phonebookType, out root))
				{
					root = new CiscoRootFolder();
					root.SetPhonebookType(phonebookType);

					m_RootsCache.Add(phonebookType, root);
				}

				return root;
			}
			finally
			{
				m_RootsSection.Leave();
			}
		}

		/// <summary>
		/// Gets the roots for every phonebook type.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<CiscoRootFolder> GetRoots()
		{
			return EnumUtils.GetValues<ePhonebookType>().Select(type => GetRoot(type));
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Called to initialize the component.
		/// </summary>
		protected override void Initialize()
		{
			base.Initialize();

			// Populate the root folders
			foreach (CiscoRootFolder root in GetRoots())
				Codec.SendCommand(root.GetSearchCommand());
		}

		/// <summary>
		/// Subscribes to the codec events.
		/// </summary>
		/// <param name="codec"></param>
		protected override void Subscribe(CiscoCodecDevice codec)
		{
			base.Subscribe(codec);

			if (codec == null)
				return;

			codec.RegisterParserCallback(ParseSearchResultAsync, "PhonebookSearchResult");
		}

		/// <summary>
		/// Unsubscribes from the codec events.
		/// </summary>
		/// <param name="codec"></param>
		protected override void Unsubscribe(CiscoCodecDevice codec)
		{
			base.Unsubscribe(codec);

			if (codec == null)
				return;

			codec.UnregisterParserCallback(ParseSearchResultAsync, "PhonebookSearchResult");
		}

		/// <summary>
		/// Asynchronously parses the search result.
		/// Useful for when we have a large result set and don't want to block the thread.
		/// </summary>
		/// <param name="codec"></param>
		/// <param name="resultid"></param>
		/// <param name="xml"></param>
		private void ParseSearchResultAsync(CiscoCodecDevice codec, string resultid, string xml)
		{
			ThreadingUtils.SafeInvoke(() => ParseSearchResult(resultid, xml));
		}

		/// <summary>
		/// Parse the search results.
		/// </summary>
		/// <param name="resultId"></param>
		/// <param name="xml"></param>
		private void ParseSearchResult(string resultId, string xml)
		{
			PhonebookSearchResult result;

			m_FolderSection.Enter();

			try
			{
				result = IcdXmlConvert.DeserializeObject<PhonebookSearchResult>(xml);
				Codec.Log(eSeverity.Debug, "Phone Book download complete. {0} entries downloaded.", result.Count);

				Insert(resultId, result.GetFolders(), result.GetContacts());
			}
			finally
			{
				m_FolderSection.Leave();
			}

			// Pre initialize this folder's children
			//foreach (CiscoFolder folder in result.GetFolders())
			//	Codec.SendCommand(folder.GetSearchCommand());

			if (OnResultParsed != null)
				OnResultParsed(resultId, result.GetFolders(), result.GetContacts());
		}

		/// <summary>
		/// Inserts the folders and contacts into the directory tree.
		/// Folders are inserted first.
		/// </summary>
		/// <param name="resultId"></param>
		/// <param name="folders"></param>
		/// <param name="contacts"></param>
		private void Insert(string resultId, IEnumerable<CiscoFolder> folders, IEnumerable<CiscoContact> contacts)
		{
			AbstractCiscoFolder parent = GetFolder(resultId);
			if (parent != null)
				parent.AddChildren(folders.Cast<IDirectoryFolder>(), contacts.Cast<IContact>());
		}

		/// <summary>
		/// Gets the root/folder with the given search id.
		/// </summary>
		/// <param name="resultId"></param>
		/// <returns></returns>
		private AbstractCiscoFolder GetFolder(string resultId)
		{
			return m_Folders.Values
							.AsEnumerable()
							.Cast<AbstractCiscoFolder>()
			                .Concat(GetRoots().Cast<AbstractCiscoFolder>())
			                .FirstOrDefault(f => f.FolderSearchId == resultId);
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new ConsoleCommand("ClearCache", "Clears the cached folders and contacts", () => Clear());
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}

	[XmlConverter(typeof(PhonebookSearchResultXmlConverter))]
	public sealed class PhonebookSearchResult
	{
		private readonly IcdOrderedDictionary<string, CiscoFolder> m_Folders;
		private readonly IcdOrderedDictionary<string, CiscoContact> m_Contacts;

		/// <summary>
		/// Gets the total number of folders and contacts.
		/// </summary>
		public int Count { get { return m_Folders.Count + m_Contacts.Count; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		public PhonebookSearchResult()
		{
			m_Folders = new IcdOrderedDictionary<string, CiscoFolder>();
			m_Contacts = new IcdOrderedDictionary<string, CiscoContact>();
		}

		/// <summary>
		/// Gets the folders.
		/// </summary>
		/// <returns></returns>
		public CiscoFolder[] GetFolders()
		{
			return m_Folders.Values.ToArray(m_Folders.Count);
		}

		/// <summary>
		/// Gets the contacts.
		/// </summary>
		/// <returns></returns>
		public CiscoContact[] GetContacts()
		{
			return m_Contacts.Values.ToArray(m_Contacts.Count);
		}

		/// <summary>
		/// Adds the given folder.
		/// </summary>
		/// <param name="folder"></param>
		public void AddFolder(CiscoFolder folder)
		{
			if (folder == null)
				throw new ArgumentNullException("folder");

			m_Folders.Add(folder.FolderId, folder);
		}

		/// <summary>
		/// Adds the given contact.
		/// </summary>
		/// <param name="contact"></param>
		public void AddContact(CiscoContact contact)
		{
			if (contact == null)
				throw new ArgumentNullException("contact");

			m_Contacts.Add(contact.ContactId, contact);
		}
	}

	public sealed class PhonebookSearchResultXmlConverter : AbstractGenericXmlConverter<PhonebookSearchResult>
	{
			// <PhonebookSearchResult item="1" status="OK">
			//   <ResultInfo item="1">
			//     <Offset item="1">0</Offset>
			//     <Limit item="1">50</Limit>
			//     <TotalRows item="1">21</TotalRows>
			//   </ResultInfo>
			//   <Folder item="1" localId="localGroupId-3">
			//     <Name item="1">CA</Name>
			//     <FolderId item="1">localGroupId-3</FolderId>
			//     <ParentFolderId item="1">localGroupId-2</ParentFolderId>
			//   </Folder>
			//   <Contact item="1">
			//     <Name item="1">Bradd Fisher</Name>
			//     <ContactId item="1">localContactId-10</ContactId>
			//     <FolderId item="1">localGroupId-18</FolderId>
			//     <Title item="1">President</Title>
			//     <ContactMethod item="1">
			//       <ContactMethodId item="1">1</ContactMethodId>
			//       <Number item="1">111</Number>
			//       <CallType item="1">Video</CallType>
			//     </ContactMethod>
			//   </Contact>
			// </PhonebookSearchResult>

		/// <summary>
		/// Override to handle the current element.
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="instance"></param>
		protected override void ReadElement(IcdXmlReader reader, PhonebookSearchResult instance)
		{
			switch (reader.Name)
			{
				case "Folder":
					CiscoFolder folder = IcdXmlConvert.DeserializeObject<CiscoFolder>(reader);
					instance.AddFolder(folder);
					break;

				case "Contact":
					CiscoContact contact = IcdXmlConvert.DeserializeObject<CiscoContact>(reader);
					instance.AddContact(contact);
					break;

				default:
					base.ReadElement(reader, instance);
					break;
			}
		}
	}
}
