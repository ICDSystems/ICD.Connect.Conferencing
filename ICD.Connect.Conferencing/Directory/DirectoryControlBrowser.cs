using System;
using ICD.Common.Properties;
using ICD.Connect.Conferencing.Contacts;
using ICD.Connect.Conferencing.Controls.Directory;
using ICD.Connect.Conferencing.Directory.Tree;

namespace ICD.Connect.Conferencing.Directory
{
	public sealed class DirectoryControlBrowser : AbstractDirectoryBrowser<IDirectoryFolder, IContact>
	{
		private IDirectoryControl m_Control;

		/// <summary>
		/// Gets the root folder.
		/// </summary>
		protected override IDirectoryFolder Root { get { return m_Control == null ? null : m_Control.GetRoot(); } }

		#region Methods

		/// <summary>
		/// Sets the directory control for browsing.
		/// </summary>
		/// <param name="control"></param>
		[PublicAPI]
		public void SetControl(IDirectoryControl control)
		{
			if (control == m_Control)
				return;

			Unsubscribe(m_Control);
			m_Control = control;
			Subscribe(m_Control);

			GoToRoot();
		}

		/// <summary>
		/// Instructs the wrapped control to populate the current folder.
		/// </summary>
		public void PopulateCurrentFolder()
		{
			if (m_Control == null)
				return;

			IDirectoryFolder current = GetCurrentFolder();
			if (current == null)
				return;

			m_Control.PopulateFolder(current);
		}

		#endregion

		#region Control Callbacks

		/// <summary>
		/// Subscribe to the component events.
		/// </summary>
		/// <param name="control"></param>
		private void Subscribe(IDirectoryControl control)
		{
			if (control == null)
				return;

			control.OnCleared += ComponentOnCleared;
		}

		/// <summary>
		/// Subscribe to the component events.
		/// </summary>
		/// <param name="control"></param>
		private void Unsubscribe(IDirectoryControl control)
		{
			if (control == null)
				return;

			control.OnCleared -= ComponentOnCleared;
		}

		/// <summary>
		/// Called when the directory is cleared.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void ComponentOnCleared(object sender, EventArgs eventArgs)
		{
			GoToRoot();
		}

		#endregion
	}
}
