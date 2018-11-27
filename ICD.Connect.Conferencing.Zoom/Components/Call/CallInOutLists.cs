using System.Collections.Generic;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Components.Call
{
	public sealed class CallInOutLists
	{
		[JsonProperty("callout_country_list")]
		public List<CallInOutListEntry> CalloutCountryList { get; private set; }

		[JsonProperty("callin_country_list")]
		public List<CallInOutListEntry> CallinCountryList { get; private set; }

		[JsonProperty("toll_free_callin_list")]
		public List<CallInOutListEntry> TollFreeCallinList { get; private set; }
	}
}