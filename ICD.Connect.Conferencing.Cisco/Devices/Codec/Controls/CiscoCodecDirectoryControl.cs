using System;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Cisco.Components.Directory;
using ICD.Connect.Conferencing.Cisco.Components.Directory.Tree;
using ICD.Connect.Conferencing.Controls.Directory;
using ICD.Connect.Conferencing.Directory.Tree;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Controls
{
	public sealed class CiscoCodecDirectoryControl : AbstractDirectoryControl<CiscoCodecDevice>
	{
		/// <summary>
		/// Raised when the directory tree is cleared.
		/// </summary>
		public override event EventHandler OnCleared;

		private readonly DirectoryComponent m_Component;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public CiscoCodecDirectoryControl(CiscoCodecDevice parent, int id)
			: base(parent, id)
		{
			m_Component = parent.Components.GetComponent<DirectoryComponent>();
			Subscribe(m_Component);
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnCleared = null;

			base.DisposeFinal(disposing);

			Unsubscribe(m_Component);
		}

		#region Methods

		/// <summary>
		/// Gets the root folder for the directory.
		/// </summary>
		/// <returns></returns>
		public override IDirectoryFolder GetRoot()
		{
			ePhonebookType type = Parent.PhonebookType;
			return m_Component.GetRoot(type);
		}

		/// <summary>
		/// Clears the cached directory for repopulation.
		/// </summary>
		public override void Clear()
		{
			m_Component.Clear();
		}

		/// <summary>
		/// Begin caching the child elements of the given folder.
		/// </summary>
		/// <param name="folder"></param>
		public override void PopulateFolder(IDirectoryFolder folder)
		{
			if (folder == null)
				throw new ArgumentNullException("folder");

			AbstractCiscoFolder ciscoFolder = folder as AbstractCiscoFolder;
			if (ciscoFolder == null)
				throw new InvalidOperationException(string.Format("{0} is not of type {1}", folder.GetType().Name,
				                                                  typeof(AbstractCiscoFolder).Name));

			m_Component.Codec.SendCommand(ciscoFolder.GetSearchCommand());
		}

		#endregion

		#region Component Callbacks

		/// <summary>
		/// Subscribe to the component events.
		/// </summary>
		/// <param name="component"></param>
		private void Subscribe(DirectoryComponent component)
		{
			component.OnCleared += ComponentOnCleared;
		}

		/// <summary>
		/// Unsubscribe from the component events.
		/// </summary>
		/// <param name="component"></param>
		private void Unsubscribe(DirectoryComponent component)
		{
			component.OnCleared -= ComponentOnCleared;
		}

		/// <summary>
		/// Called when the component clears the cached directory structure.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void ComponentOnCleared(object sender, EventArgs eventArgs)
		{
			OnCleared.Raise(this);
		}

		#endregion
	}
}
