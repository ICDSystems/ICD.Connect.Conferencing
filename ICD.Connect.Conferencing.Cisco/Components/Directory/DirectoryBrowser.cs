using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Conferencing.Cisco.Components.Directory.Tree;

namespace ICD.Connect.Conferencing.Cisco.Components.Directory
{
	/// <summary>
	/// DirectoryBrowser provides methods for browsing through a phonebook.
	/// </summary>
	public sealed class DirectoryBrowser : IDisposable
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
		private readonly Stack<IFolder> m_Path;

		/// <summary>
		/// Tracks folders that have been populated via browsing.
		/// </summary>
		private readonly IcdHashSet<IFolder> m_Populated;

		private readonly SafeCriticalSection m_PathSection;
		private readonly SafeCriticalSection m_PopulatedSection;
		private readonly SafeCriticalSection m_NavigateSection;

		private readonly DirectoryComponent m_Component;

		private ePhonebookType m_PhonebookType;

		#region Properties

		/// <summary>
		/// Gets the phonebook type.
		/// </summary>
		[PublicAPI]
		public ePhonebookType PhonebookType
		{
			get { return m_PhonebookType; }
			set
			{
				if (value == m_PhonebookType)
					return;

				m_PhonebookType = value;

				RootFolder root = m_Component.GetRoot(m_PhonebookType);
				GoToRoot(root);
			}
		}

		/// <summary>
		/// Returns true if the current folder is the root.
		/// </summary>
		/// <value></value>
		public bool IsCurrentFolderRoot { get { return m_Path.Count == 1; } }

		/// <summary>
		/// Gets the current path as a human readable string.
		/// </summary>
		[PublicAPI]
		public string PathAsString
		{
			get { return m_PathSection.Execute(() => string.Join("/", m_Path.Select(x => x.Name).ToArray())); }
		}

		/// <summary>
		/// Gets the root folder.
		/// </summary>
		private IFolder Root { get { return m_PathSection.Execute(() => m_Path.Last()); } }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="component"></param>
		public DirectoryBrowser(DirectoryComponent component)
		{
			m_Path = new Stack<IFolder>();
			m_Populated = new IcdHashSet<IFolder>();

			m_PathSection = new SafeCriticalSection();
			m_PopulatedSection = new SafeCriticalSection();
			m_NavigateSection = new SafeCriticalSection();

			m_Component = component;
			Subscribe(m_Component);

			RootFolder root = component.GetRoot(m_PhonebookType);
			GoToRoot(root);
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
			Unsubscribe(m_Component);
		}

		/// <summary>
		/// Gets the current folder on the path.
		/// </summary>
		/// <returns></returns>
		public IFolder GetCurrentFolder()
		{
			return m_PathSection.Execute(() => m_Path.Count > 0 ? m_Path.Peek() : null);
		}

		/// <summary>
		/// Populates the current folder if it hasn't been populated yet.
		/// </summary>
		public void PopulateCurrentFolder()
		{
			IFolder folder = GetCurrentFolder();
			if (folder != null)
				PopulateFolder(folder);
		}

		/// <summary>
		/// Pushes the folder onto the path.
		/// </summary>
		/// <param name="folder"></param>
		/// <returns>The new current folder.</returns>
		[PublicAPI]
		public IFolder EnterFolder(IFolder folder)
		{
			if (folder == GetCurrentFolder())
				return folder;

			IFolder output;

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
		public IFolder GoUp()
		{
			if (IsCurrentFolderRoot)
				return GoToRoot();

			IFolder output;

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
		public IFolder GoToRoot()
		{
			return IsCurrentFolderRoot ? GetCurrentFolder() : GoToRoot(Root);
		}

		/// <summary>
		/// Clears the path and sets the given folder as the root.
		/// </summary>
		/// <param name="root"></param>
		/// <returns></returns>
		private IFolder GoToRoot(IFolder root)
		{
			m_Component.Codec.Log(eSeverity.Debug, "Going to Top level of Phone Book");

			IFolder output;

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
		private void PopulateFolder(IFolder parent)
		{
			if (m_PopulatedSection.Execute(() => m_Populated.Add(parent)))
				m_Component.Codec.SendCommand(parent.GetSearchCommand());
		}

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
		private void Subscribe(IFolder folder)
		{
			if (folder == null)
				return;

			folder.OnContentsChanged += CurrentFolderContentsChanged;
		}

		/// <summary>
		/// Unsubscribes from the current folder events.
		/// </summary>
		/// <param name="folder"></param>
		private void Unsubscribe(IFolder folder)
		{
			if (folder == null)
				return;

			folder.OnContentsChanged -= CurrentFolderContentsChanged;
		}

		/// <summary>
		/// Subscribe to the component events.
		/// </summary>
		/// <param name="component"></param>
		private void Subscribe(DirectoryComponent component)
		{
			if (component == null)
				return;

			component.OnCleared += ComponentOnCleared;
		}

		/// <summary>
		/// Subscribe to the component events.
		/// </summary>
		/// <param name="component"></param>
		private void Unsubscribe(DirectoryComponent component)
		{
			if (component == null)
				return;

			component.OnCleared -= ComponentOnCleared;
		}

		/// <summary>
		/// Called when the directory is cleared.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void ComponentOnCleared(object sender, EventArgs eventArgs)
		{
			m_PopulatedSection.Execute(() => m_Populated.Clear());
			GoToRoot();
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
