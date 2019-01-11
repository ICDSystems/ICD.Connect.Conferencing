using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.Controls.Dialing;

namespace ICD.Connect.Conferencing.EventArguments
{

	public sealed class ConferenceProviderInfo
	{
		public eCallType ProviderType { get; set; }

		public IConferenceDeviceControl Provider { get; set; }
	}

	public sealed class ConferenceProviderEventArgs : GenericEventArgs<ConferenceProviderInfo>
	{
		public eCallType ProviderType { get { return Data.ProviderType; } }

		public IConferenceDeviceControl Provider { get { return Data.Provider; } }

		public ConferenceProviderEventArgs(eCallType providerType, IConferenceDeviceControl provider)
			: base(new ConferenceProviderInfo {Provider = provider, ProviderType = providerType})
		{
			
		}
	}
}