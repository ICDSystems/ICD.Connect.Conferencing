using System;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Conferencing.Cisco.Components.Directory.Tree;

namespace ICD.Connect.Conferencing.Cisco.Components.Directory
{
	/// <summary>
	/// DirectorySearch provides methods for searching a phonebook.
	/// </summary>
	[PublicAPI]
	public sealed class DirectorySearch : IDisposable
	{
		/// <summary>
		/// The max number of results for a search.
		/// </summary>
		private const int SEARCH_LIMIT = 50;

		/// <summary>
		/// Called when the search results change.
		/// </summary>
		[PublicAPI]
		public event EventHandler OnSearchResultsUpdated;

		// We store the most recent search id so we can parse out the correct results.
		private string m_SearchId;
		private readonly IcdHashSet<IFolder> m_FolderSearchResults;
		private readonly IcdHashSet<CiscoContact> m_ContactSearchResults;

		/// <summary>
		/// Gets the directory component.
		/// </summary>
		private readonly DirectoryComponent m_Component;

		#region Properties

		/// <summary>
		/// Gets the number of folder search results.
		/// </summary>
		[PublicAPI]
		public int FolderSearchResultsCount { get { return m_FolderSearchResults.Count; } }

		/// <summary>
		/// Gets the number of contact search results.
		/// </summary>
		[PublicAPI]
		public int ContactSearchResultsCount { get { return m_ContactSearchResults.Count; } }

		/// <summary>
		/// Gets the total number of search results.
		/// </summary>
		[PublicAPI]
		public int SearchResultsCount { get { return FolderSearchResultsCount + ContactSearchResultsCount; } }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="component"></param>
		public DirectorySearch(DirectoryComponent component)
		{
			m_ContactSearchResults = new IcdHashSet<CiscoContact>();
			m_FolderSearchResults = new IcdHashSet<IFolder>();

			m_Component = component;

			Subscribe(m_Component);
		}

		#endregion

		#region Methods

		/// <summary>
		/// Releases resources.
		/// </summary>
		public void Dispose()
		{
			Unsubscribe(m_Component);
		}

		/// <summary>
		/// Searches the entire directory for items matching the given string.
		/// </summary>
		/// <param name="searchString"></param>
		/// <param name="phonebookType"></param>
		/// <param name="contactType"></param>
		[PublicAPI]
		public void Search(string searchString, ePhonebookType phonebookType, eContactType contactType)
		{
			Clear();

			if (string.IsNullOrEmpty(searchString))
				return;

			m_SearchId = Guid.NewGuid().ToString();
			searchString = searchString.Trim();

			m_Component.Codec.SendCommand("xCommand Phonebook Search PhonebookType: {0} ContactType: {1} Limit: {2} " +
			                              "SearchString: \"{3}\" | resultId=\"{4}\"",
			                              phonebookType, contactType, SEARCH_LIMIT, searchString, m_SearchId);

			m_Component.Codec.Log(eSeverity.Debug, "Requesting Phone book Type: {0} Limit: {0}", phonebookType, SEARCH_LIMIT);
		}

		/// <summary>
		/// Gets the folder search results.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		public IFolder[] GetFolderSearchResults()
		{
			return m_FolderSearchResults.OrderBy(f => f.Name).ToArray();
		}

		/// <summary>
		/// Gets the folder search result at the given index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		[PublicAPI]
		public IFolder GetFolderSearchResult(int index)
		{
			return GetFolderSearchResults()[index];
		}

		/// <summary>
		/// Gets the contact search result at the given index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		[PublicAPI]
		public CiscoContact GetContactSearchResult(int index)
		{
			return GetContactSearchResults()[index];
		}

		/// <summary>
		/// Gets the contact search results.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		public CiscoContact[] GetContactSearchResults()
		{
			return m_ContactSearchResults.OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ToArray();
		}

		/// <summary>
		/// Gets the search result at the given index. Folders come first.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		[PublicAPI]
		public INode GetSearchResult(int index)
		{
			if (index < FolderSearchResultsCount)
				return GetFolderSearchResult(index);

			index -= FolderSearchResultsCount;
			return GetContactSearchResult(index);
		}

		#endregion

		#region Private Methods

		private void Clear()
		{
			m_SearchId = null;
			m_FolderSearchResults.Clear();
			m_ContactSearchResults.Clear();

			RaiseSearchResultsUpdate();
		}

		private void RaiseSearchResultsUpdate()
		{
			OnSearchResultsUpdated.Raise(this);
		}

		#endregion

		#region Component Callbacks

		private void Subscribe(DirectoryComponent component)
		{
			if (component == null)
				return;

			component.OnCleared += ComponentOnCleared;
			component.OnResultParsed += ComponentOnResultParsed;
		}

		private void Unsubscribe(DirectoryComponent component)
		{
			if (component == null)
				return;

			component.OnCleared -= ComponentOnCleared;
			component.OnResultParsed -= ComponentOnResultParsed;
		}

		private void ComponentOnCleared(object sender, EventArgs eventArgs)
		{
			Clear();
		}

		private void ComponentOnResultParsed(string resultId, IFolder[] folders, CiscoContact[] contacts)
		{
			if (resultId != m_SearchId)
				return;

			m_FolderSearchResults.AddRange(folders);
			m_ContactSearchResults.AddRange(contacts);
			RaiseSearchResultsUpdate();
		}

		#endregion
	}
}
