using ICD.Common.Utils.EventArguments;
using System;
using System.Collections.Generic;
using System.Text;

namespace ICD.Connect.Conferencing.Zoom.Components.Presentation
{
	public class PresentationOutputEventArgs : GenericEventArgs<int?>
	{
		public PresentationOutputEventArgs(int? data) : base(data)
		{
		}
	}
}
