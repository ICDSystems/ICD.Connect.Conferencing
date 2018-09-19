namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Directory.Tree
{
	/// <summary>
	/// RootFolder represents a root folder in the phonebook (corporate or local)
	/// </summary>
	public sealed class CiscoRootFolder : AbstractCiscoFolder
	{
		private ePhonebookType m_PhonebookType;

		#region Properties

		/// <summary>
		/// Gets the phonebook type.
		/// </summary>
		public override ePhonebookType PhonebookType { get { return m_PhonebookType; } }

		/// <summary>
		/// Gets the name of the root (i.e. the phonebook type)
		/// </summary>
		public override string Name { get { return PhonebookType.ToString(); } }

		#endregion

		/// <summary>
		/// Sets the phonebook type.
		/// </summary>
		/// <param name="phonebookType"></param>
		public void SetPhonebookType(ePhonebookType phonebookType)
		{
			m_PhonebookType = phonebookType;
		}
	}
}
