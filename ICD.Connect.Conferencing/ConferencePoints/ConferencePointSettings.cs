using ICD.Common.Properties;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Conferencing.ConferencePoints
{
	[KrangSettings(FACTORY_NAME, typeof(ConferencePoint))]
	public sealed class ConferencePointSettings : AbstractConferencePointSettings
	{
		[PublicAPI("MetLife settings pages")]
		public const string FACTORY_NAME = "ConferencePoint";
	}
}