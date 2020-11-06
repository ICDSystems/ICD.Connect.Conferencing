using System;
using ICD.Connect.API.Attributes;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Proxies.Controls.DirectSharing;
using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Conferencing.Controls.DirectSharing
{
	[ApiClass(typeof(ProxyDirectSharingControl), typeof(IDeviceControl))]
	public interface IDirectSharingControl : IDeviceControl
	{
		/// <summary>
		/// Raised when the direct sharing enabled state changes.
		/// </summary>
		[ApiEvent(DirectSharingControlApi.EVENT_DIRECT_SHARING_ENABLED, DirectSharingControlApi.EVENT_HELP_DIRECT_SHARING_ENABLED)]
		event EventHandler<DirectSharingEnabledApiEventArgs> OnDirectShareEnabledChanged;

		/// <summary>
		/// Raised when the direct sharing active state changes.
		/// </summary>
		[ApiEvent(DirectSharingControlApi.EVENT_DIRECT_SHARING_ACTIVE, DirectSharingControlApi.EVENT_HELP_DIRECT_SHARING_ACTIVE)]
		event EventHandler<DirectSharingActiveApiEventArgs> OnDirectShareActiveChanged;

		/// <summary>
		/// Raised when the direct sharing code changes.
		/// </summary>
		[ApiEvent(DirectSharingControlApi.EVENT_SHARING_CODE, DirectSharingControlApi.EVENT_HELP_SHARING_CODE)]
		event EventHandler<DirectSharingCodeApiEventArgs> OnSharingCodeChanged;

		/// <summary>
		/// Raised when the direct sharing source name changes.
		/// </summary>
		[ApiEvent(DirectSharingControlApi.EVENT_SHARING_SOURCE_NAME, DirectSharingControlApi.EVENT_HELP_SHARING_SOURCE_NAME)]
		event EventHandler<DirectSharingSourceNameApiEventArgs> OnSharingSourceNameChagned;

		/// <summary>
		/// Whether or not direct sharing is configured on the device.
		/// </summary>
		[ApiProperty(DirectSharingControlApi.PROPERTY_DIRECT_SHARING_ENABLED, DirectSharingControlApi.PROPERTY_HELP_DIRECT_SHARING_ENABLED)]
		bool DirectShareEnabled { get; }

		/// <summary>
		/// Whether or not a user is currently sharing content.
		/// </summary>
		[ApiProperty(DirectSharingControlApi.PROPERTY_DIRECT_SHARING_ACTIVE, DirectSharingControlApi.PROPERTY_HELP_DIRECT_SHARING_ACTIVE)]
		bool DirectShareActive { get; }

		/// <summary>
		/// Some direct sharing devices have a code needed to share.
		/// </summary>
		[ApiProperty(DirectSharingControlApi.PROPERTY_DIRECT_SHARING_CODE, DirectSharingControlApi.PROPERTY_HELP_SHARING_CODE)]
		string SharingCode { get; }

		/// <summary>
		/// The name of the connected device currently sharing content.
		/// </summary>
		[ApiProperty(DirectSharingControlApi.PROPERTY_DIRECT_SHARING_SOURCE_NAME, DirectSharingControlApi.PROPERTY_HELP_SHARING_SOURCE_NAME)]
		string SharingSourceName { get; }
	}
}
