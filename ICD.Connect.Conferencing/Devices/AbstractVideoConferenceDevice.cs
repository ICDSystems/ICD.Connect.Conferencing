using ICD.Connect.Devices;
using ICD.Connect.Settings;

namespace ICD.Connect.Conferencing.Devices
{
	public abstract class AbstractVideoConferenceDevice<TSettings> : AbstractDevice<TSettings>, IVideoConferenceDevice
		where TSettings : IVideoConferenceDeviceSettings, new()
	{
		private readonly CodecInputTypes m_InputTypes;

		/// <summary>
		/// Configured information about how the input connectors should be used.
		/// </summary>
		public CodecInputTypes InputTypes { get { return m_InputTypes; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		protected AbstractVideoConferenceDevice()
		{
			m_InputTypes = new CodecInputTypes();
		}

		#region Settings

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(TSettings settings)
		{
			base.CopySettingsFinal(settings);

			InputTypes.CopySettings(settings);
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			InputTypes.ClearSettings();
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(TSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			InputTypes.ApplySettings(settings);
		}

		#endregion
	}
}
