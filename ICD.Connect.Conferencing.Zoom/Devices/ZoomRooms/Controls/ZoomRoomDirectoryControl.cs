﻿using System;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Controls.Directory;
using ICD.Connect.Conferencing.Directory.Tree;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.Directory;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Controls
{
	public sealed class ZoomRoomDirectoryControl : AbstractDirectoryControl<ZoomRoom>
	{
		public override event EventHandler OnCleared;

		private readonly DirectoryComponent m_Component;

		public ZoomRoomDirectoryControl(ZoomRoom parent, int id)
			: base(parent, id)
		{
			m_Component = parent.Components.GetComponent<DirectoryComponent>();
		}

		protected override void DisposeFinal(bool disposing)
		{
			OnCleared = null;

			base.DisposeFinal(disposing);
		}

		public override IDirectoryFolder GetRoot()
		{
			return m_Component.GetRoot();
		}

		public override void Clear()
		{
			m_Component.GetRoot().Clear();
			OnCleared.Raise(this);
		}

		public override void PopulateFolder(IDirectoryFolder folder)
		{
			if (!(folder is ZoomFolder))
				throw new InvalidOperationException("Cannot populate folder unless it is ZoomFolder");
			m_Component.Populate();
		}
	}
}