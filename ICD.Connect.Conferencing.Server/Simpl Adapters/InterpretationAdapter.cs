using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Controls;
using ICD.Connect.Devices;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Conferencing.Server
{
	public sealed class InterpretationAdapter : AbstractDevice<InterpretationAdapterSettings>, IInterpretationAdapter
	{

		public event EventHandler<GenericEventArgs<IDialingDeviceControl>> OnDialerAdded;
		public event EventHandler<GenericEventArgs<IDialingDeviceControl>> OnDialerRemoved;

		#region Private Memebers

		#endregion

		#region Public Properties

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public InterpretationAdapter()
		{

		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);
		}

		#region Public Methods

		public Dictionary<IDialingDeviceClientControl, string> GetDialingControls()
		{
			throw new NotImplementedException();
		}

		#endregion

		#region Private Helper Methods

		#endregion

		#region Settings

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(InterpretationAdapterSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(InterpretationAdapterSettings settings)
		{
			base.CopySettingsFinal(settings);
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();
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
		}

		#endregion

		#region IDevice

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
