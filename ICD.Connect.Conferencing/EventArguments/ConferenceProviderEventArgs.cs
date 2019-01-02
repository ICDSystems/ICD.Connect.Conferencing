using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.Controls.Dialing;

namespace ICD.Connect.Conferencing.EventArguments
{

	public sealed class ConferenceProviderInfo
	{
		public eConferenceSourceType ProviderType { get; set; }

		public IDialingDeviceControl Provider { get; set; }
	}

	public sealed class ConferenceProviderEventArgs : GenericEventArgs<ConferenceProviderInfo>
	{
		public eConferenceSourceType ProviderType {get { return Data.ProviderType; }}

		public IDialingDeviceControl Provider {get { return Data.Provider; }}

		public ConferenceProviderEventArgs(eConferenceSourceType providerType, IDialingDeviceControl provider)
			: base(new ConferenceProviderInfo {Provider = provider, ProviderType = providerType})
		{
			
		}
	}
}