namespace ICD.Connect.Conferencing.Cisco.Components.Directory.Tree
{
	/// <summary>
	/// RootFolder represents a root folder in the phonebook (corporate or local)
	/// </summary>
	public sealed class CiscoRootFolder : AbstractCiscoFolder
	{
		private readonly ePhonebookType m_PhonebookType;

		#region Properties

		/// <summary>
		/// Gets the phonebook type.
		/// </summary>
		public ePhonebookType PhonebookType { get { return m_PhonebookType; } }

		/// <summary>
		/// Gets the name of the root (i.e. the phonebook type)
		/// </summary>
		public override string Name { get { return PhonebookType.ToString(); } }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="phonebookType"></param>
		public CiscoRootFolder(ePhonebookType phonebookType) : base(null)
		{
			m_PhonebookType = phonebookType;
		}

		#endregion
	}
}
