using System;
using System.Collections.Generic;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Conferencing.Controls.DirectSharing
{
	public static class DirectSharingControlConsole
	{
		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static IEnumerable<IConsoleNodeBase> GetConsoleNodes(IDirectSharingControl instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			yield break;
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="addRow"></param>
		public static void BuildConsoleStatus(IDirectSharingControl instance, AddStatusRowDelegate addRow)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			addRow("Direct Sharing Enabled", instance.DirectShareEnabled);
			addRow("Direct Sharing Active", instance.DirectShareActive);
			addRow("Sharing Code", instance.SharingCode);
			addRow("Sharing Source Name", instance.SharingSourceName);
		}
	}
}
