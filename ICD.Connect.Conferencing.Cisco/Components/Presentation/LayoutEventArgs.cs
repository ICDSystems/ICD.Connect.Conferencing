﻿using ICD.Common.EventArguments;
using ICD.Common.Properties;

namespace ICD.Connect.Conferencing.Cisco.Components.Presentation
{
	// Ignore missing comments
#pragma warning disable 1591
	public enum eLayout
	{
		[PublicAPI] Default,
		[PublicAPI] Maximized,
		[PublicAPI] Minimized
	}
#pragma warning restore 1591

	/// <summary>
	/// Event args for use with layout events.
	/// </summary>
	public sealed class LayoutEventArgs : GenericEventArgs<eLayout>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public LayoutEventArgs(eLayout data) : base(data)
		{
		}
	}
}
