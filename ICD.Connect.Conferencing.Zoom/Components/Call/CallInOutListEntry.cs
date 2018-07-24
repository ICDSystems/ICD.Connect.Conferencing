using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Components.Call
{
	public sealed class CallInOutListEntry
	{
		[JsonProperty("id")]
		public string Id { get; private set; }

		[JsonProperty("name")]
		public string Name { get; private set; }

		[JsonProperty("code")]
		public string Code { get; private set; }

		[JsonProperty("number")]
		public string Number { get; private set; }

		[JsonProperty("display_number")]
		public string DisplayNumber { get; private set; }
	}
}