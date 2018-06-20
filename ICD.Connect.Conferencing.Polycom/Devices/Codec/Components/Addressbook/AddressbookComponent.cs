﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.Conferencing.Contacts;
using ICD.Connect.Conferencing.Directory.Tree;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Addressbook
{
	public sealed class AddressbookComponent : AbstractPolycomComponent
	{
		private const string CONTACT_REGEX =
			@"addrbook \d+. ""(?'name'[^""]*)""( (?'speedprot'[^_]+)_spd:(?'speed'[^\s]*))? (?'prot'[^_]+)_num:(?'number'[^\s]*)( (?'extprot'[^_]+)_ext:(?'ext'\d+)?)?";

		/// <summary>
		/// Called when the cache is cleared.
		/// </summary>
		public event EventHandler OnCleared;

		private readonly Dictionary<eAddressbookType, RootFolder> m_RootsCache;
		private readonly SafeCriticalSection m_RootsSection;

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

			Codec.SendCommand("addrbook all");

			// "gaddrbook all" isn't working on my test unit :(
			foreach (char c in GetValidAddressbookLetters())
				Codec.SendCommand("gaddrbook letter {0}", c);
		}

		#region Methods

		/// <summary>
		/// Clears the cache.
		/// </summary>
		public void Clear()
		{
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
			throw new NotImplementedException();
		}

		#endregion

		/// <summary>
		/// Gets the addressbook letters that are valid for the "gaddrbook letter {0}" command.
		/// </summary>
		/// <returns></returns>
		private static IEnumerable<char> GetValidAddressbookLetters()
		{
			for (char c = '0'; c <= '9'; c++)
				yield return c;

			for (char c = 'a'; c <= 'z'; c++)
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

			IContact contact = ParseContact(data);
			AddContact(eAddressbookType.Local, contact);
		}

		private void HandleGlobalDir(string data)
		{
			// gaddrbook 0. "Chris VanLuvanee" sip_spd:Auto sip_num:chris.van@profoundtech.onmicrosoft.com
			// gaddrbook 1. "ConfRoom" sip_spd:Auto sip_num:confroom@profoundtech.onmicrosoft.com
			// gaddrbook letter C done

			if (data.EndsWith(" done") || data.EndsWith(" none"))
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

			IContactMethod[] contactMethods =
			{
				new ContactMethod(match.Groups["number"].Value) 
			};

			return new Contact(match.Groups["name"].Value, contactMethods);
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

			string lastName = contact.Name
			                         .Split()
			                         .FirstOrDefault(s => !string.IsNullOrEmpty(s));

			if (string.IsNullOrEmpty(lastName))
				return;

			char letter = lastName.First();

			RootFolder root = GetRoot(addressbookType);
			IDirectoryFolder folder = root.GetFolders().FirstOrDefault(f => f.Name == letter.ToString());

			if (folder == null)
			{
				folder = new DirectoryFolder(letter.ToString());
				root.AddFolder(folder);
			}

			folder.AddContact(contact);
		}

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
