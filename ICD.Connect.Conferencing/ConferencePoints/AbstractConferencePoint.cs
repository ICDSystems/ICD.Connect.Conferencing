﻿using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.VolumePoints;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Devices.Points;
using ICD.Connect.Settings;

namespace ICD.Connect.Conferencing.ConferencePoints
{
	public abstract class AbstractConferencePoint<TSettings> : AbstractPoint<TSettings, IConferenceDeviceControl>, IConferencePoint
		where TSettings : IConferencePointSettings, new()
	{
		#region Properties

		/// <summary>
		/// Gets the category for this originator type (e.g. Device, Port, etc)
		/// </summary>
		public override string Category { get { return "ConferencePoint"; } }

		/// <summary>
		/// The type of call to use the conference control for.
		/// </summary>
		public eCallType Type { get; set; }

		/// <summary>
		/// Determines if the privacy mute control will be driven by the control system, and/or drive the control system.
		/// </summary>
		public ePrivacyMuteFeedback PrivacyMuteMask { get; set; }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		protected AbstractConferencePoint()
		{
			PrivacyMuteMask = ePrivacyMuteFeedback.Set;
		}

		#region Settings

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(TSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Type = Type;
			settings.PrivacyMuteMask = PrivacyMuteMask;
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(TSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			Type = settings.Type;
			PrivacyMuteMask = settings.PrivacyMuteMask;
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();
			
			Type = eCallType.Unknown;
			PrivacyMuteMask = ePrivacyMuteFeedback.Set;
		}

		#endregion

		#region Console

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Type", Type);
			addRow("Privacy Mute Mask", PrivacyMuteMask);
		}

		#endregion
	}
}
