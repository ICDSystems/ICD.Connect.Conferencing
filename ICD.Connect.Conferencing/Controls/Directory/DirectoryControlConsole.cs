using System;
using System.Collections.Generic;
using System.Text;
using ICD.Common.Utils;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Contacts;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.Directory.Tree;

namespace ICD.Connect.Conferencing.Controls.Directory
{
	public static class DirectoryControlConsole
	{
		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static IEnumerable<IConsoleNodeBase> GetConsoleNodes(IDirectoryControl instance)
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
		public static void BuildConsoleStatus(IDirectoryControl instance, AddStatusRowDelegate addRow)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static IEnumerable<IConsoleCommand> GetConsoleCommands(IDirectoryControl instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			yield return new ConsoleCommand("PrintContacts", "Prints a listing of the cached contacts", () => PrintContacts(instance));
			yield return new ConsoleCommand("Clear", "Clears the cached directory structure", () => instance.Clear());
		}

		private static string PrintContacts(IDirectoryControl instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			StringBuilder builder = new StringBuilder();
			IDirectoryFolder root = instance.GetRoot();

			PrintFolder(builder, root, 0);

			return builder.ToString();
		}

		private static void PrintFolder(StringBuilder builder, IDirectoryFolder folder, int depth)
		{
			if (builder == null)
				throw new ArgumentNullException("builder");

			if (folder == null)
				throw new ArgumentNullException("folder");

			string tab = StringUtils.Repeat('\t', depth);

			// Add the folder
			builder.Append(tab);
			builder.AppendLine(folder.Name);

			// Add the child folders
			foreach (IDirectoryFolder child in folder.GetFolders())
				PrintFolder(builder, child, depth + 1);

			// Add the child contacts
			foreach (IContact child in folder.GetContacts())
				PrintContact(builder, child, depth + 1);
		}

		private static void PrintContact(StringBuilder builder, IContact contact, int depth)
		{
			if (builder == null)
				throw new ArgumentNullException("builder");

			if (contact == null)
				throw new ArgumentNullException("contact");

			string tab = StringUtils.Repeat('\t', depth);

			// Add the contact
			builder.Append(tab);
			builder.AppendLine(contact.Name);

			// Add the contact methods
			foreach (IDialContext child in contact.GetDialContexts())
				PrintDialContext(builder, child, depth + 1);
		}

		private static void PrintDialContext(StringBuilder builder, IDialContext dialContext, int depth)
		{
			if (builder == null)
				throw new ArgumentNullException("builder");

			if (dialContext == null)
				throw new ArgumentNullException("dialContext");

			string tab = StringUtils.Repeat('\t', depth);

			// Add the contact method
			builder.Append(tab);
			builder.AppendLine(dialContext.DialString);
		}
	}
}
