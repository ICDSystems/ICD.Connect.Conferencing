using ICD.Common.Logging.LoggingContexts;
using ICD.Common.Utils.Services.Logging;

namespace ICD.Connect.Conferencing.Controls.Dialing
{
	public static class ConferenceDeviceControlActivities
	{
		public static Activity GetHoldActivity(bool hold)
		{
			return hold
				       ? new Activity(Activity.ePriority.Medium, "Hold", "On Hold", eSeverity.Informational)
				       : new Activity(Activity.ePriority.Low, "Hold", "Not On Hold", eSeverity.Informational);
		}

		public static Activity GetPrivacyMuteActivity(bool privacyMuted)
		{
			return privacyMuted
				       ? new Activity(Activity.ePriority.Medium, "Privacy Mute", "Privacy Muted", eSeverity.Informational)
				       : new Activity(Activity.ePriority.Low, "Privacy Mute", "Not Privacy Muted", eSeverity.Informational);
		}
	}
}
