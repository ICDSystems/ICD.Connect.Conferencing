namespace ICD.Connect.Conferencing.Cisco.Components.Directory.Tree
{
	// Ignore missing comments
#pragma warning disable 1591
	public enum ePhonebookType
	{
		Corporate,
		Local
	}
#pragma warning restore 1591

	/// <summary>
	/// Interface for both folder and contacts.
	/// </summary>
	public interface INode
	{
		/// <summary>
		/// Gets the name.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets the folder id.
		/// </summary>
		string FolderId { get; }

		/// <summary>
		/// Gets the phonebook type.
		/// </summary>
		ePhonebookType PhonebookType { get; }
	}
}
