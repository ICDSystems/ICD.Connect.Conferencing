using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Devices.Points;
using ICD.Connect.Settings;

namespace ICD.Connect.Conferencing.ConferencePoints
{
	public abstract class AbstractConferencePoint<TSettings> : AbstractPoint<TSettings>, IConferencePoint
		where TSettings : IConferencePointSettings, new()
	{
		#region Properties

		public eCallType Type { get; set; }

		#endregion

		#region Settings

		protected override void CopySettingsFinal(TSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Type = Type;
		}

		protected override void ApplySettingsFinal(TSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			Type = settings.Type;
		}

		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();
			
			Type = eCallType.Unknown;
		}

		#endregion

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Type", Type);
		}
	}
}
