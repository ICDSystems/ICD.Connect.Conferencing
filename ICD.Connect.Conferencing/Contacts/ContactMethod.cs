namespace ICD.Connect.Conferencing.Contacts
{
	public sealed class ContactMethod : IContactMethod
	{
		private readonly string m_Number;

		/// <summary>
		/// Gets the contact number.
		/// </summary>
		public string Number { get { return m_Number; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="number"></param>
		public ContactMethod(string number)
		{
			m_Number = number;
		}
	}
}
