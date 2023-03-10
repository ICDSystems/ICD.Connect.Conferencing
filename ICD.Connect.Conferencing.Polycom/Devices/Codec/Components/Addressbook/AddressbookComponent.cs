using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.Conferencing.Contacts;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.Directory.Tree;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Addressbook
{
	public sealed class AddressbookComponent : AbstractPolycomComponent
	{
		private const string CONTACT_REGEX =
			@"addrbook \d+\. ""(?'name'[^""]*)""( ((?'speedprot'[^_]+)_)?spd:(?'speed'[^\s]*))? ((?'prot'[^_]+)_)?num:(?'number'[^\s]*)( ((?'extprot'[^_]+)_)?ext:(?'ext'\d+)?)?";

		private const string GADDRBOOK_DONE_REGEX =
			@"gaddrbook letter (?'letter'\S) (done|none)";

		/// <summary>
		/// Called when the cache is cleared.
		/// </summary>
		public event EventHandler OnCleared;

		/// <summary>
		/// Chains addressbook letters, A->B, B->C, etc
		/// </summary>
		private static readonly Dictionary<char, char> s_NextChar; 

		private readonly Dictionary<eAddressbookType, RootFolder> m_RootsCache;
		private readonly SafeCriticalSection m_RootsSection;

		/// <summary>
		/// True while we are looping through the letter entries for global directories
		/// </summary>
		private bool m_PopulatingGlobalRoot;

		/// <summary>
		/// Static Constructor.
		/// </summary>
		static AddressbookComponent()
		{
			s_NextChar = new Dictionary<char, char>();

			char last = default(char);
			bool first = true;

			foreach (char letter in GetValidAddressbookLetters())
			{
				if (!first)
					s_NextChar[last] = letter;

				last = letter;
				first = false;
			}
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="codec"></param>
		public AddressbookComponent(PolycomGroupSeriesDevice codec)
			: base(codec)
		{
			m_RootsCache = new Dictionary<eAddressbookType, RootFolder>();
			m_RootsSection = new SafeCriticalSection();

			Subscribe(Codec);

			codec.RegisterFeedback("addrbook", HandleLocalDir);
			codec.RegisterFeedback("gaddrbook", HandleGlobalDir);

			if (Codec.Initialized)
				Initialize();
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			OnCleared = null;

			base.Dispose(disposing);
		}

		/// <summary>
		/// Called to initialize the component.
		/// </summary>
		protected override void Initialize()
		{
			base.Initialize();

			m_PopulatingGlobalRoot = false;

			PopulateLocalAddressbook();

			//if (!m_PopulatingGlobalRoot)
			//	PopulateGlobalAddressbook();
		}

		#region Methods

		/// <summary>
		/// Clears the cache.
		/// </summary>
		public void Clear()
		{
			m_PopulatingGlobalRoot = false;

			m_RootsSection.Enter();

			try
			{
				foreach (RootFolder root in GetRoots())
					root.Clear();
			}
			finally
			{
				m_RootsSection.Leave();
			}

			OnCleared.Raise(this);
		}

		/// <summary>
		/// Gets the root for the configured addressbook type.
		/// </summary>
		/// <returns></returns>
		public RootFolder GetRoot()
		{
			return GetRoot(Codec.AddressbookType);
		}

		/// <summary>
		/// Gets the root for the given addressbook type.
		/// </summary>
		/// <param name="addressbookType"></param>
		/// <returns></returns>
		public RootFolder GetRoot(eAddressbookType addressbookType)
		{
			m_RootsSection.Enter();

			try
			{
				if (!m_RootsCache.ContainsKey(addressbookType))
					m_RootsCache[addressbookType] = new RootFolder(addressbookType);

				return m_RootsCache[addressbookType];
			}
			finally
			{
				m_RootsSection.Leave();
			}
		}

		/// <summary>
		/// Gets the roots for every addressbook type.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<RootFolder> GetRoots()
		{
			return EnumUtils.GetValues<eAddressbookType>().Select(type => GetRoot(type));
		}

		/// <summary>
		/// Begin caching the child elements of the given folder.
		/// </summary>
		/// <param name="folder"></param>
		public void PopulateFolder(IDirectoryFolder folder)
		{
			if (folder == null)
				throw new ArgumentNullException("folder");

			// Prevent spamming the device with directory requests
			if (folder.ChildCount > 0)
				return;

			RootFolder root = folder as RootFolder;
			if (root != null)
			{
				switch (root.Type)
				{
					case eAddressbookType.Local:
						PopulateLocalAddressbook();
						return;
						/*
					case eAddressbookType.Global:
						if (!m_PopulatingGlobalRoot)
							PopulateGlobalAddressbook();
						return;
						 */
					default:
						throw new NotSupportedException();
				}
			}

			root = GetRoots().First(r => r.ContainsFolder(folder));

			switch (root.Type)
			{
				case eAddressbookType.Local:
					Codec.EnqueueCommand("addrbook letter {0}", folder.Name);
					break;
				/*
				case eAddressbookType.Global:
					if (!m_PopulatingGlobalRoot)
						PopulateGlobalAddressbook(folder.Name.First());
					break;
				 */
				default:
					throw new NotSupportedException();
			}
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Sends commands to the system to pull down the local addressbook.
		/// </summary>
		private void PopulateLocalAddressbook()
		{
			Codec.EnqueueCommand("addrbook all");
		}

		/// <summary>
		/// Sends commands to the system to pull down the global addressbook.
		/// </summary>
		private void PopulateGlobalAddressbook()
		{
			throw new NotSupportedException();

			m_PopulatingGlobalRoot = true;

			// "gaddrbook all" isn't working on my test unit :(
			// We enqueue the first letter and then we'll enqueue
			// the next when a response is returned.
			PopulateGlobalAddressbook(GetValidAddressbookLetters().First());
		}

		/// <summary>
		/// Sends commands to the system to pull down the global addressbook.
		/// </summary>
		private void PopulateGlobalAddressbook(char letter)
		{
			throw new NotSupportedException();

			m_PopulatingGlobalRoot = true;

			Codec.EnqueueCommand("gaddrbook letter {0}", letter);
		}

		/// <summary>
		/// Gets the addressbook letters that are valid for the "gaddrbook letter {0}" command.
		/// </summary>
		/// <returns></returns>
		private static IEnumerable<char> GetValidAddressbookLetters()
		{
			for (char c = '0'; c <= '9'; c++)
				yield return c;

			for (char c = 'A'; c <= 'Z'; c++)
				yield return c;

			foreach (char c in new [] {'-', '/', ';', '@', ',', '.', '\\'})
				yield return c;
		}

		private void HandleLocalDir(string data)
		{
			// addrbook 0. "Polycom Group Series Demo 1" isdn_spd:384 isdn_num:1.700.5551212 isdn_ext:
			// addrbook 1. "Polycom Group Series Demo 2" h323_spd: 384 h323_num: 192.168.1.101 h323_ext: 7878
			// addrbook 2. "Polycom Group Series Demo 3" sip_spd: 384 sip_num: polycomgroupseries @polycom.com
			// addrbook 3. "Polycom Group Series Demo 3" phone_num: 1.512.5121212
			// addrbook all done

			if (data.EndsWith(" done"))
				return;

			if (data.StartsWith("addrbook all"))
				return;

			IContact contact = ParseContact(data);
			AddContact(eAddressbookType.Local, contact);
		}

		private void HandleGlobalDir(string data)
		{
			// Not supported
			return;

			// gaddrbook 0. "Chris VanLuvanee" sip_spd:Auto sip_num:chris.van@profoundtech.onmicrosoft.com
			// gaddrbook 1. "ConfRoom" sip_spd:Auto sip_num:confroom@profoundtech.onmicrosoft.com
			// gaddrbook letter C done

			// Enqueue the next letter
			if (data.EndsWith(" done") || data.EndsWith(" none"))
			{
				Match match = Regex.Match(data, GADDRBOOK_DONE_REGEX);
				if (match.Success)
				{
					char letter = match.Groups["letter"].Value.First();
					if (s_NextChar.ContainsKey(letter))
						PopulateGlobalAddressbook(s_NextChar[letter]);
					else
						m_PopulatingGlobalRoot = false;
				}

				return;
			}

			if (data.StartsWith("gaddrbook letter "))
				return;

			IContact contact = ParseContact(data);
			AddContact(eAddressbookType.Global, contact);
		}

		/// <summary>
		/// Parses the contact data into a contact instance.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		private static IContact ParseContact(string data)
		{
			Match match = Regex.Match(data, CONTACT_REGEX);
			if (!match.Success)
				throw new FormatException("Failed to parse contact");

			Contact contact = new Contact
			{
				Name = match.Groups["name"].Value,
				DialContexts = new List<IDialContext>
				{
					new DialContext {DialString = match.Groups["number"].Value}
				}
			};

			return contact;
		}

		/// <summary>
		/// Adds the contact to the directory tree.
		/// </summary>
		/// <param name="addressbookType"></param>
		/// <param name="contact"></param>
		private void AddContact(eAddressbookType addressbookType, IContact contact)
		{
			if (contact == null)
				throw new ArgumentNullException("contact");

			if (string.IsNullOrEmpty(contact.Name))
				return;

			RootFolder root = GetRoot(addressbookType);

			/*char letter = contact.Name.First();
			letter = char.ToUpper(letter);
			
			IDirectoryFolder folder = root.GetFolders().FirstOrDefault(f => f.Name == letter.ToString());

			if (folder == null)
			{
				string folderName = string.Format("Search by letter: {0}", letter);
				folder = new DirectoryFolder(folderName);

				root.AddFolder(folder);
			}*/

			root.AddContact(contact);
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
}
