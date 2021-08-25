#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using System.Linq;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.Bookings;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters
{
	public sealed class BookingsListCommandResponseConverter : AbstractZoomRoomResponseConverter<BookingsListCommandResponse>
	{
		private const string ATTR_BOOKINGS_LIST_RESULT = "BookingsListResult";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, BookingsListCommandResponse value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.Bookings != null && value.Bookings.Length > 0)
			{
				writer.WritePropertyName(ATTR_BOOKINGS_LIST_RESULT);
				serializer.SerializeArray(writer, value.Bookings);
			}
		}

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