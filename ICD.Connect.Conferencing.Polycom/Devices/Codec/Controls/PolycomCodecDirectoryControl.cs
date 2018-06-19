using System;
using ICD.Connect.Conferencing.Controls.Directory;
using ICD.Connect.Conferencing.Directory.Tree;
using ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Addressbook;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec.Controls
{
	public sealed class PolycomCodecDirectoryControl : AbstractDirectoryControl<PolycomGroupSeriesDevice>
	{
		/// <summary>
		/// Raised when the directory tree is cleared.
		/// </summary>
		public override event EventHandler OnCleared;

		private readonly AddressbookComponent m_AddressbookComponent;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public PolycomCodecDirectoryControl(PolycomGroupSeriesDevice parent, int id)
			: base(parent, id)
		{
			m_AddressbookComponent = parent.Components.GetComponent<AddressbookComponent>();

			Subscribe(m_AddressbookComponent);
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnCleared = null;

			base.DisposeFinal(disposing);

			Unsubscribe(m_AddressbookComponent);
		}

		#region Methods

		/// <summary>
		/// Gets the root folder for the directory.
		/// </summary>
		/// <returns></returns>
		public override IDirectoryFolder GetRoot()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Clears the cached directory for repopulation.
		/// </summary>
		public override void Clear()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Begin caching the child elements of the given folder.
		/// </summary>
		/// <param name="folder"></param>
		public override void PopulateFolder(IDirectoryFolder folder)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region Addressbook Component Callbacks

		/// <summary>
		/// Subscribe to the addressbook component events.
		/// </summary>
		/// <param name="addressbookComponent"></param>
		private void Subscribe(AddressbookComponent addressbookComponent)
		{
		}

		/// <summary>
		/// Unsubscribe from the addressbook component events.
		/// </summary>
		/// <param name="addressbookComponent"></param>
		private void Unsubscribe(AddressbookComponent addressbookComponent)
		{
		}

		#endregion
	}
}
