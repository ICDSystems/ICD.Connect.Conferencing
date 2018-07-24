using System;
using ICD.Connect.Conferencing.Controls.Presentation;

namespace ICD.Connect.Conferencing.Zoom.Controls
{
	public class ZoomRoomPresentationControl : AbstractPresentationControl<ZoomRoom>
	{
		public ZoomRoomPresentationControl(ZoomRoom parent, int id) : base(parent, id)
		{
		}

		public override void StartPresentation(int input)
		{
			
		}

		public override void StopPresentation()
		{
			throw new NotImplementedException();
		}
	}
}