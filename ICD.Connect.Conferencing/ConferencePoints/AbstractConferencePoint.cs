using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Originators;

namespace ICD.Connect.Conferencing.ConferencePoints
{
	public abstract class AbstractConferencePoint<TSettings> : AbstractOriginator<TSettings>, IConferencePoint
		where TSettings : IConferencePointSettings, new()
	{
		#region Properties

		/// <summary>
		/// Device id
		/// </summary>
		public int DeviceId { get; set; }

		/// <summary>
		/// Control id.
		/// </summary>
		public int ControlId { get; set; }

		public eCallType Type { get; set; }

		#endregion

		#region Settings

		protected override void CopySettingsFinal(TSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.DeviceId = DeviceId;
			settings.ControlId = ControlId;
			settings.Type = Type;
		}

		protected override void ApplySettingsFinal(TSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			DeviceId = settings.DeviceId;
			ControlId = settings.ControlId;
			Type = settings.Type;
		}

		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			DeviceId = 0;
			ControlId = 0;
			Type = eCallType.Unknown;
		}

		#endregion
	}
}
