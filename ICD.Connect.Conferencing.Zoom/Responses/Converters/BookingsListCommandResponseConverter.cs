using System.Linq;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using ICD.Connect.Conferencing.Zoom.Components.Bookings;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses.Converters
{
	public sealed class BookingsListCommandResponseConverter : AbstractGenericJsonConverter<BookingsListCommandResponse>
	{
		private const string ATTR_BOOKINGS_LIST_RESULT = "BookingsListResult";

		protected override void ReadProperty(string property, JsonReader reader, BookingsListCommandResponse instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_BOOKINGS_LIST_RESULT:
					instance.Bookings = serializer.DeserializeArray<Booking>(reader).ToArray();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}