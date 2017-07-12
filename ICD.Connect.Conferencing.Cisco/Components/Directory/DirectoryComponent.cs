﻿using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Services.Logging;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Xml;
using ICD.Connect.API.Commands;
using ICD.Connect.Conferencing.Cisco.Components.Directory.Tree;

namespace ICD.Connect.Conferencing.Cisco.Components.Directory
{
	// Ignore missing comment warnings.
#pragma warning disable 1591
	public enum eContactType
	{
		[PublicAPI] Any,
		[PublicAPI] Folder,
		[PublicAPI] Contact
	}
#pragma warning restore 1591

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
		public delegate void ResultParsedDelegate(string resultId, IFolder[] folders, CiscoContact[] contacts);

		/// <summary>
		/// Called when the cache is cleared.
		/// </summary>
		public event EventHandler OnCleared;

		/// <summary>
		/// Called when a result is parsed.
		/// </summary>
		public event ResultParsedDelegate OnResultParsed;

		// Mapping folder/contact ids
		private readonly Dictionary<string, IFolder> m_Folders;
		private readonly Dictionary<string, CiscoContact> m_Contacts;

		private readonly Dictionary<ePhonebookType, RootFolder> m_RootsCache;

		private readonly SafeCriticalSection m_FolderSection;
		private readonly SafeCriticalSection m_RootsSection;

// ReSharper disable NotAccessedField.Local
		private object m_ParseAsyncHandle;
// ReSharper restore NotAccessedField.Local

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="codec"></param>
		public DirectoryComponent(CiscoCodec codec)
			: base(codec)
		{
			m_RootsCache = new Dictionary<ePhonebookType, RootFolder>();
			m_Folders = new Dictionary<string, IFolder>();
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
		public override void Dispose()
		{
			OnCleared = null;
			OnResultParsed = null;

			m_ParseAsyncHandle = null;

			base.Dispose();
		}

		/// <summary>
		/// Clears the cache.
		/// </summary>
		public void Clear()
		{
			m_FolderSection.Enter();

			try
			{
				foreach (RootFolder root in GetRoots())
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
		/// Gets the root for the given phonebook type.
		/// </summary>
		/// <param name="phonebookType"></param>
		/// <returns></returns>
		public RootFolder GetRoot(ePhonebookType phonebookType)
		{
			m_RootsSection.Enter();

			try
			{
				if (!m_RootsCache.ContainsKey(phonebookType))
					m_RootsCache[phonebookType] = new RootFolder(phonebookType);

				return m_RootsCache[phonebookType];
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
		public IEnumerable<RootFolder> GetRoots()
		{
			return EnumUtils.GetValues<ePhonebookType>().Select(type => GetRoot(type));
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Subscribes to the codec events.
		/// </summary>
		/// <param name="codec"></param>
		protected override void Subscribe(CiscoCodec codec)
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
		protected override void Unsubscribe(CiscoCodec codec)
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
		private void ParseSearchResultAsync(CiscoCodec codec, string resultid, string xml)
		{
			m_ParseAsyncHandle = CrestronUtils.SafeInvoke(() => ParseSearchResult(resultid, xml));
		}

		/// <summary>
		/// Parse the search results.
		/// </summary>
		/// <param name="resultId"></param>
		/// <param name="xml"></param>
		private void ParseSearchResult(string resultId, string xml)
		{
			IcdHashSet<IFolder> folders = new IcdHashSet<IFolder>();
			IcdHashSet<CiscoContact> contacts = new IcdHashSet<CiscoContact>();

			m_FolderSection.Enter();

			try
			{
				folders.AddRange(XmlUtils.GetChildElementsAsString(xml, "Folder")
				                         .Select(e => Folder.FromXml(e, resultId, m_Folders) as IFolder));

				contacts.AddRange(XmlUtils.GetChildElementsAsString(xml, "Contact")
				                          .Select(e => CiscoContact.FromXml(e, resultId, m_Contacts)));

				int count = folders.Count + contacts.Count;
				Codec.Log(eSeverity.Debug, "Phone Book download complete. {0} entries downloaded.", count);

				Insert(resultId, folders, contacts);
			}
			finally
			{
				m_FolderSection.Leave();
			}

			if (OnResultParsed != null)
				OnResultParsed(resultId, folders.ToArray(), contacts.ToArray());
		}

		/// <summary>
		/// Inserts the folders and contacts into the directory tree.
		/// Folders are inserted first.
		/// </summary>
		/// <param name="resultId"></param>
		/// <param name="folders"></param>
		/// <param name="contacts"></param>
		private void Insert(string resultId, IEnumerable<IFolder> folders, IEnumerable<CiscoContact> contacts)
		{
			IFolder parent = GetFolder(resultId);
			if (parent != null)
				parent.AddChildren(folders, contacts);
		}

		/// <summary>
		/// Gets the root/folder with the given search id.
		/// </summary>
		/// <param name="resultId"></param>
		/// <returns></returns>
		private IFolder GetFolder(string resultId)
		{
			return m_Folders.Values.AsEnumerable()
			                .Concat(GetRoots().Cast<IFolder>())
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
}
