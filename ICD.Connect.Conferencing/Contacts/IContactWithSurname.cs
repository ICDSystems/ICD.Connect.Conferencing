namespace ICD.Connect.Conferencing.Contacts
{
	public interface IContactWithSurname : IContact
	{
		string FirstName { get; }

		string LastName { get; }
	}
}