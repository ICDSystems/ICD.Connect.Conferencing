using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Contacts;
using ICD.Connect.Conferencing.Directory.Tree;

namespace ICD.Connect.Conferencing.Directory
{

	/// <summary>
	/// DirectoryBrowser provides methods for browsing through a phonebook.
	/// </summary>
	public abstract class AbstractDirectoryBrowser<TFolder, TContact> : IDirectoryBrowser<TFolder, TContact>
		where TFolder : class, IFolder
		where TContact : class, IContact
	{
		/// <summary>
		/// Called when navigating to a different folder.
		/// </summary>
		public event EventHandler<FolderEventArgs> OnPathChanged;

		/// <summary>
		/// Called when the contents of the current folder change.
		/// </summary>
		public event EventHandler OnPathContentsChanged;

		/// <summary>
		/// The current path for browsing.
		/// </summary>
		protected readonly Stack<TFolder> m_Path;

		/// <summary>
		/// Tracks folders that have been populated via browsing.
		/// </summary>
		protected readonly IcdHashSet<TFolder> m_Populated;

		protected readonly SafeCriticalSection m_PathSection;
		protected readonly SafeCriticalSection m_PopulatedSection;
		protected readonly SafeCriticalSection m_NavigateSection;

		#region Properties

		/// <summary>
		/// Returns true if the current folder is the root.
		/// </summary>
		/// <value></value>
		public bool IsCurrentFolderRoot { get { return m_Path.Count == 1; } }

		/// <summary>
		/// Gets the root folder.
		/// </summary>
		protected TFolder Root { get { return m_PathSection.Execute(() => m_Path.Last()); } }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		public AbstractDirectoryBrowser()
		{
			m_Path = new Stack<TFolder>();
			m_Populated = new IcdHashSet<TFolder>();

			m_PathSection = new SafeCriticalSection();
			m_PopulatedSection = new SafeCriticalSection();
			m_NavigateSection = new SafeCriticalSection();
		}

		#endregion

		#region Method

		/// <summary>
		/// Releases resources.
		/// </summary>
		public void Dispose()
		{
			OnPathChanged = null;
			OnPathContentsChanged = null;

			if (m_Path.Count > 0)
				Unsubscribe(m_Path.Peek());
		}

		/// <summary>
		/// Gets the current folder on the path.
		/// </summary>
		/// <returns></returns>
		public TFolder GetCurrentFolder()
		{
			return m_PathSection.Execute(() => m_Path.Count > 0 ? m_Path.Peek() : default(TFolder));
		}

		/// <summary>
		/// Populates the current folder if it hasn't been populated yet.
		/// </summary>
		public void PopulateCurrentFolder()
		{
			TFolder folder = GetCurrentFolder();
			if (folder != null)
				PopulateFolder(folder);
		}

		/// <summary>
		/// Pushes the folder onto the path.
		/// </summary>
		/// <param name="folder"></param>
		/// <returns>The new current folder.</returns>
		[PublicAPI]
		public TFolder EnterFolder(TFolder folder)
		{
			if (folder == GetCurrentFolder())
				return folder;

			TFolder output;

			m_NavigateSection.Enter();

			try
			{
				Unsubscribe(GetCurrentFolder());
				m_PathSection.Execute(() => m_Path.Push(folder));
				output = GetCurrentFolder();
				Subscribe(output);
			}
			finally
			{
				m_NavigateSection.Leave();
			}

			RaiseOnPathChanged();

			return output;
		}

		/// <summary>
		/// Sets the parent folder as the current folder.
		/// </summary>
		/// <returns>The new current folder.</returns>
		[PublicAPI]
		public TFolder GoUp()
		{
			if (IsCurrentFolderRoot)
				return GoToRoot();

			TFolder output;

			m_NavigateSection.Enter();

			try
			{
				Unsubscribe(GetCurrentFolder());
				m_PathSection.Execute(() => m_Path.Pop());
				output = GetCurrentFolder();
				Subscribe(output);
			}
			finally
			{
				m_NavigateSection.Leave();
			}

			RaiseOnPathChanged();

			return output;
		}

		/// <summary>
		/// Sets the root folder as the current folder.
		/// </summary>
		/// <returns></returns>
		/// <returns>The new current folder.</returns>
		public TFolder GoToRoot()
		{
			return IsCurrentFolderRoot ? GetCurrentFolder() : GoToRoot(Root);
		}

		/// <summary>
		/// Clears the path and sets the given folder as the root.
		/// </summary>
		/// <param name="root"></param>
		/// <returns></returns>
		protected TFolder GoToRoot(TFolder root)
		{
			TFolder output;

			m_NavigateSection.Enter();

			try
			{
				Unsubscribe(GetCurrentFolder());

				m_PathSection.Enter();

				try
				{
					m_Path.Clear();
					m_Path.Push(root);
				}
				finally
				{
					m_PathSection.Leave();
				}

				output = GetCurrentFolder();
				Subscribe(output);
			}
			finally
			{
				m_NavigateSection.Leave();
			}

			RaiseOnPathChanged();

			return output;
		}

		#endregion

		#region Private Method

		/// <summary>
		/// Sends the command to begin populating the folder.
		/// </summary>
		protected abstract void PopulateFolder(TFolder parent);

		/// <summary>
		/// Raises the OnPathChanged event.
		/// </summary>
		private void RaiseOnPathChanged()
		{
			IFolder current = GetCurrentFolder();
			OnPathChanged.Raise(this, new FolderEventArgs(current));
		}

		/// <summary>
		/// Subscribes to the current folder events.
		/// </summary>
		/// <param name="folder"></param>
		protected virtual void Subscribe(TFolder folder)
		{
			if (folder == null)
				return;

			folder.OnContentsChanged += CurrentFolderContentsChanged;
		}

		/// <summary>
		/// Unsubscribes from the current folder events.
		/// </summary>
		/// <param name="folder"></param>
		protected virtual void Unsubscribe(TFolder folder)
		{
			if (folder == null)
				return;

			folder.OnContentsChanged -= CurrentFolderContentsChanged;
		}

		/// <summary>
		/// Called when the current folder contents change.
		/// </summary>
		private void CurrentFolderContentsChanged(object sender, EventArgs e)
		{
			OnPathContentsChanged.Raise(this);
		}

		#endregion
	}
}
