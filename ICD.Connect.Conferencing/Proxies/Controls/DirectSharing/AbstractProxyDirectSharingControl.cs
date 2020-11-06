using System;
using System.Collections.Generic;
using ICD.Common.Logging.LoggingContexts;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API;
using ICD.Connect.API.Info;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Controls.DirectSharing;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Devices.Proxies.Controls;
using ICD.Connect.Devices.Proxies.Devices;

namespace ICD.Connect.Conferencing.Proxies.Controls.DirectSharing
{
	public abstract class AbstractProxyDirectSharingControl : AbstractProxyDeviceControl, IProxyDirectSharingControl
	{
		#region Events

		/// <summary>
		/// Raised when the direct sharing enabled state changes.
		/// </summary>
		public event EventHandler<DirectSharingEnabledApiEventArgs> OnDirectShareEnabledChanged;

		/// <summary>
		/// Raised when the direct sharing active state changes.
		/// </summary>
		public event EventHandler<DirectSharingActiveApiEventArgs> OnDirectShareActiveChanged;

		/// <summary>
		/// Raised when the direct sharing code changes.
		/// </summary>
		public event EventHandler<DirectSharingCodeApiEventArgs> OnSharingCodeChanged;

		/// <summary>
		/// Raised when the direct sharing source name changes.
		/// </summary>
		public event EventHandler<DirectSharingSourceNameApiEventArgs> OnSharingSourceNameChagned;

		#endregion

		private bool m_DirectShareEnabled;
		private bool m_DirectShareActive;
		private string m_SharingCode;
		private string m_SharingSourceName;

		#region Properties

		/// <summary>
		/// Whether or not direct sharing is configured on the device.
		/// </summary>
		public bool DirectShareEnabled
		{
			get { return m_DirectShareEnabled; }
			protected set
			{
				if (value == m_DirectShareEnabled)
					return;

				m_DirectShareEnabled = value;

				Logger.LogSetTo(eSeverity.Informational, "DirectShareEnabled", m_DirectShareEnabled);

				OnDirectShareEnabledChanged.Raise(this, new DirectSharingEnabledApiEventArgs(m_DirectShareEnabled));
			}
		}

		/// <summary>
		/// Whether or not a user is currently sharing content.
		/// </summary>
		public bool DirectShareActive
		{
			get { return m_DirectShareActive; }
			protected set
			{
				if (value == m_DirectShareActive)
					return;

				m_DirectShareActive = value;

				Logger.LogSetTo(eSeverity.Informational, "DirectShareActive", m_DirectShareActive);

				OnDirectShareActiveChanged.Raise(this, new DirectSharingActiveApiEventArgs(m_DirectShareActive));
			}
		}

		/// <summary>
		/// Some direct sharing devices have a code needed to share.
		/// </summary>
		public string SharingCode
		{
			get { return m_SharingCode; }
			protected set
			{
				if (value == m_SharingCode)
					return;

				m_SharingCode = value;

				Logger.LogSetTo(eSeverity.Informational, "SharingCode", m_SharingCode);

				OnSharingCodeChanged.Raise(this, new DirectSharingCodeApiEventArgs(m_SharingCode));
			}
		}

		/// <summary>
		/// The name of the connected device currently sharing content.
		/// </summary>
		public string SharingSourceName
		{
			get { return m_SharingSourceName; }
			protected set
			{
				if (value == m_SharingSourceName)
					return;

				m_SharingSourceName = value;

				Logger.LogSetTo(eSeverity.Informational, "SharingSourceName", m_SharingSourceName);

				OnSharingSourceNameChagned.Raise(this, new DirectSharingSourceNameApiEventArgs(m_SharingSourceName));
			}
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		protected AbstractProxyDirectSharingControl([NotNull] IProxyDevice parent, int id)
			: base(parent, id)
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		/// <param name="uuid"></param>
		protected AbstractProxyDirectSharingControl([NotNull] IProxyDevice parent, int id, Guid uuid)
			: base(parent, id, uuid)
		{
		}

		#endregion

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnDirectShareEnabledChanged = null;
			OnDirectShareActiveChanged = null;
			OnSharingCodeChanged = null;
			OnSharingSourceNameChagned = null;

			base.DisposeFinal(disposing);
		}

		protected override void Initialize(ApiClassInfo command)
		{
			base.Initialize(command);

			ApiCommandBuilder.UpdateCommand(command)
			                 .SubscribeEvent(DirectSharingControlApi.EVENT_DIRECT_SHARING_ENABLED)
			                 .SubscribeEvent(DirectSharingControlApi.EVENT_DIRECT_SHARING_ACTIVE)
			                 .SubscribeEvent(DirectSharingControlApi.EVENT_SHARING_CODE)
			                 .SubscribeEvent(DirectSharingControlApi.EVENT_SHARING_SOURCE_NAME)
			                 .GetProperty(DirectSharingControlApi.PROPERTY_DIRECT_SHARING_ENABLED)
			                 .GetProperty(DirectSharingControlApi.PROPERTY_DIRECT_SHARING_ACTIVE)
			                 .GetProperty(DirectSharingControlApi.PROPERTY_DIRECT_SHARING_CODE)
			                 .GetProperty(DirectSharingControlApi.PROPERTY_DIRECT_SHARING_SOURCE_NAME)
			                 .Complete();
		}

		#region Console

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			return DirectSharingControlConsole.GetConsoleNodes(this);
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			DirectSharingControlConsole.BuildConsoleStatus(this, addRow);
		}

		#endregion
	}
}
