﻿using ICD.Connect.API.EventArguments;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.Proxies.Controls.Dialing;

namespace ICD.Connect.Conferencing.EventArguments
{
	public sealed class ConferenceControlSupportedConferenceFeaturesChangedApiEventArgs :
		AbstractGenericApiEventArgs<eConferenceControlFeatures>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public ConferenceControlSupportedConferenceFeaturesChangedApiEventArgs(eConferenceControlFeatures data)
			: base(ConferenceDeviceControlApi.EVENT_SUPPORTED_CONFERENCE_FEATURES_CHANGED, data)
		{
		}
	}
}
