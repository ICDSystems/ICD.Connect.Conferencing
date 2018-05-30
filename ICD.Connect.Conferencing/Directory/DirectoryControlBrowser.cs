using System;
using ICD.Common.Properties;
using ICD.Connect.Conferencing.Contacts;
using ICD.Connect.Conferencing.Controls;
using ICD.Connect.Conferencing.Directory.Tree;

namespace ICD.Connect.Conferencing.Directory
{
	public sealed class DirectoryControlBrowser : AbstractDirectoryBrowser<IDirectoryFolder, IContact>
	{
		private IDirectoryControl m_Control;

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

			m_Control = control;

			GoToRoot();
		}

		#endregion

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
			Clear();
		}
	}
}
