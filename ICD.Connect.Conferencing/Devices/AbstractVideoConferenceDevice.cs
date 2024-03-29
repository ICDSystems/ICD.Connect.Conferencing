﻿using System.Collections.Generic;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
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
		/// The default camera used by the conference device.
		/// </summary>
		public IDeviceBase DefaultCamera { get; set; }

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
			settings.DefaultCameraDevice = DefaultCamera == null ? (int?)null : DefaultCamera.Id;
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			InputTypes.ClearSettings();
			DefaultCamera = null;
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

			IDeviceBase defaultCamera = null;
			if (settings.DefaultCameraDevice != null)
			{
				try
				{
					defaultCamera = factory.GetOriginatorById(settings.DefaultCameraDevice.Value) as IDeviceBase;
				}
				catch (KeyNotFoundException)
				{
					Logger.Log(eSeverity.Error, "No default camera device with id {0}", settings.DefaultCameraDevice);
				}
			}

			DefaultCamera = defaultCamera;
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

			VideoConferenceDeviceConsole.BuildConsoleStatus(this, addRow);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			foreach (IConsoleCommand command in VideoConferenceDeviceConsole.GetConsoleCommands(this))
				yield return command;
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			foreach (IConsoleNodeBase node in VideoConferenceDeviceConsole.GetConsoleNodes(this))
				yield return node;
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
		{
			return base.GetConsoleNodes();
		}

		#endregion
	}
}
