using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Conferencing.Cisco.Components.Directory.Tree;
using ICD.Connect.Conferencing.Directory;
using ICD.Connect.Conferencing.Directory.Tree;

namespace ICD.Connect.Conferencing.Cisco.Components.Directory
{
	/// <summary>
	/// DirectoryBrowser provides methods for browsing through a phonebook.
	/// </summary>
	public sealed class CiscoDirectoryBrowser : AbstractDirectoryBrowser<AbstractCiscoFolder, CiscoContact>
	{

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

				CiscoRootFolder root = new CiscoRootFolder(m_PhonebookType);
				GoToRoot(root);
			}
		}

		/// <summary>
		/// Gets the current path as a human readable string.
		/// </summary>
		[PublicAPI]
		public string PathAsString
		{
			get { return m_PathSection.Execute(() => string.Join("/", m_Path.Select<AbstractCiscoFolder, string>(x => x.Name).ToArray())); }
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="component"></param>
		public CiscoDirectoryBrowser(DirectoryComponent component) : base()
		{
			m_Component = component;
			Subscribe(m_Component);

			CiscoRootFolder root = component.GetRoot(m_PhonebookType);
			GoToRoot(root);
		}

		#endregion

		#region Private Method

		/// <summary>
		/// Sends the command to begin populating the folder.
		/// </summary>
		protected override void PopulateFolder(AbstractCiscoFolder parent)
		{
			if (m_PopulatedSection.Execute(() => m_Populated.Add(parent)) && parent.ChildCount == 0)
				m_Component.Codec.SendCommand(parent.GetSearchCommand());
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

		#endregion
	}
}
