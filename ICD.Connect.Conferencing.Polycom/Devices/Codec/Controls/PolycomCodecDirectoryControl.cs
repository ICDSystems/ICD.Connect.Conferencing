using System;
using ICD.Connect.Conferencing.Controls.Directory;
using ICD.Connect.Conferencing.Directory.Tree;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec.Controls
{
	public sealed class PolycomCodecDirectoryControl : AbstractDirectoryControl<PolycomGroupSeriesDevice>
	{
		/// <summary>
		/// Raised when the directory tree is cleared.
		/// </summary>
		public override event EventHandler OnCleared;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public PolycomCodecDirectoryControl(PolycomGroupSeriesDevice parent, int id)
			: base(parent, id)
		{
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnCleared = null;

			base.DisposeFinal(disposing);
		}

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
	}
}
