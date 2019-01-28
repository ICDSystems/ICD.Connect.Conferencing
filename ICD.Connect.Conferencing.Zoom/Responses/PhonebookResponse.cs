using System.Linq;
using ICD.Connect.Conferencing.Zoom.Components.Directory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("Phonebook", eZoomRoomApiType.zEvent, false)]
	public sealed class PhonebookContactUpdatedResponse : AbstractZoomRoomResponse
	{
		[JsonProperty("Phonebook")]
		public PhonebookUpdatedContact Data { get; private set; }

		public override void LoadFromJObject(JObject jObject)
		{
			base.LoadFromJObject(jObject);

			Data = new PhonebookUpdatedContact();
			Data.LoadFromJObject((JObject) jObject["Phonebook"]);
		}
	}

	[ZoomRoomApiResponse("PhonebookListResult", eZoomRoomApiType.zCommand, true)]
	public sealed class PhonebookListCommandResponse : AbstractZoomRoomResponse
	{
		[JsonProperty("PhonebookListResult")]
		public PhonebookListResult PhonebookListResult { get; private set; }

		public override void LoadFromJObject(JObject jObject)
		{
			base.LoadFromJObject(jObject);

			PhonebookListResult = new PhonebookListResult();
			PhonebookListResult.LoadFromJObject((JObject) jObject["PhonebookListResult"]);
		}
	}

	public sealed class PhonebookListResult : AbstractZoomRoomData
	{
		[JsonProperty("Contacts")]
		public ZoomContact[] Contacts { get; private set; }

		[JsonProperty("Limit")]
		public int Limit { get; private set; }

		[JsonProperty("Offset")]
		public int Offset { get; private set; }

		public override void LoadFromJObject(JObject jObject)
		{
			Contacts = jObject["Contacts"].Children().Select(o =>
			{
				var contact = new ZoomContact();
				contact.LoadFromJObject((JObject) o);
				return contact;
			}).ToArray();

			Limit = jObject["Limit"].ToObject<int>();
			Offset = jObject["Offset"].ToObject<int>();
		}
	}

	public sealed class PhonebookUpdatedContact : AbstractZoomRoomData
	{
		[JsonProperty("Updated Contact")]
		public ZoomContact Contact { get; private set; }

		public override void LoadFromJObject(JObject jObject)
		{
			Contact = new ZoomContact();
			if (jObject["Updated Contact"] != null)
				Contact.LoadFromJObject((JObject) jObject["Updated Contact"]);
			else if (jObject["Added Contact"] != null)
				Contact.LoadFromJObject((JObject) jObject["Added Contact"]);
		}
	}
}