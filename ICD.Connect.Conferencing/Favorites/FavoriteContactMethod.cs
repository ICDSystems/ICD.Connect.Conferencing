using ICD.Connect.Conferencing.Contacts;

namespace ICD.Connect.Conferencing.Favorites
{
	public sealed class FavoriteContactMethod : IContactMethod
	{
		/// <summary>
		/// Gets the table id for this instance.
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// Gets/sets the contact number.
		/// </summary>
		public string Number { get; set; }

		/// <summary>
		/// Instantiates the FavoriteContactMethod from the given contact method.
		/// </summary>
		/// <param name="contactMethod"></param>
		/// <returns></returns>
		public static FavoriteContactMethod FromContactMethod(IContactMethod contactMethod)
		{
			return new FavoriteContactMethod {Number = contactMethod.Number};
		}
	}
}
