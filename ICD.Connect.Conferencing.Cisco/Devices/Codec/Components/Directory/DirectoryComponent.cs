using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Xml;
using ICD.Connect.API.Commands;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Directory.Tree;
using ICD.Connect.Conferencing.Contacts;
using ICD.Connect.Conferencing.Directory.Tree;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Directory
{
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
		/// Callback for individual contact parsing.
		/// </summary>
		/// <param name="contact"></param>
		public delegate void ContactParsedDelegate(CiscoContact contact);

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

		public void PhonebookSearch(ePhonebookType phonebookType, string searchString, eSearchFilter searchFilter, int limit)
		{
			// Deserialize the phonebook result and call the callback for each contact
			CiscoCodecDevice.ParserCallback wrapper = (codec, id, xml) =>
				IcdXmlConvert.DeserializeObject<PhonebookSearchResult>(xml);

			Codec.SendCommand("xCommand Phonebook Search PhonebookType: {0} SearchString: {1} SearchFilter: {2} Limit: {3}", wrapper,
			                  phonebookType, searchString, searchFilter, limit);
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

				m_Folders.Clear();
			}
			finally
			{
				m_FolderSection.Leave();
			}

			OnCleared.Raise(this);
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
			//foreach (CiscoRootFolder root in GetRoots())
			//	Codec.SendCommand(root.GetSearchCommand());
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
				Codec.Logger.Log(eSeverity.Debug, "Phone Book download complete. {0} entries downloaded.", result.Count);

				Insert(resultId, result.GetFolders(), result.GetContacts());
			}
			finally
			{
				m_FolderSection.Leave();
			}

			ResultParsedDelegate handler = OnResultParsed;
			if (handler != null)
				handler(resultId, result.GetFolders(), result.GetContacts());
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
			if (folders == null)
				throw new ArgumentNullException("folders");

			if (contacts == null)
				throw new ArgumentNullException("contacts");

			IList<CiscoFolder> foldersArray = folders as IList<CiscoFolder> ?? folders.ToArray();
			IList<CiscoContact> contactsArray = contacts as IList<CiscoContact> ?? contacts.ToArray();

			m_FolderSection.Enter();

			try
			{
				foreach (CiscoFolder folder in foldersArray)
					m_Folders[folder.FolderSearchId] = folder;
			}
			finally
			{
				m_FolderSection.Leave();
			}

			AbstractCiscoFolder parent = GetFolder(resultId);
			if (parent != null)
				parent.AddChildren(foldersArray.Cast<IDirectoryFolder>(), contactsArray.Cast<IContact>());
		}

		/// <summary>
		/// Gets the root/folder with the given search id.
		/// </summary>
		/// <param name="resultId"></param>
		/// <returns></returns>
		private AbstractCiscoFolder GetFolder(string resultId)
		{
			return m_FolderSection.Execute(() => m_Folders.GetDefault(resultId)) 
				?? m_RootsCache.Values.FirstOrDefault(value => value.FolderSearchId == resultId) as AbstractCiscoFolder;
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
			yield return new GenericConsoleCommand<string, ePhonebookType, int>("Search", "Search <string> <phonebooktype> <limit>", (s,p,l) => PhonebookSearch(p, s, eSearchFilter.All, l));
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

	
}
