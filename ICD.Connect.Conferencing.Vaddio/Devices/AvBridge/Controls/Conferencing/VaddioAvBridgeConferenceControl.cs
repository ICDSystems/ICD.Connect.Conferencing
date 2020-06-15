using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.IncomingCalls;
using ICD.Connect.Conferencing.Vaddio.Devices.AvBridge.Components.Audio;
using ICD.Connect.Conferencing.Vaddio.Devices.AvBridge.Components.Video;

namespace ICD.Connect.Conferencing.Vaddio.Devices.AvBridge.Controls.Conferencing
{
	public sealed class VaddioAvBridgeConferenceControl : AbstractConferenceDeviceControl<VaddioAvBridgeDevice, VaddioAvBridgeWebinar>
	{
		#region Events

		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallAdded;
		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallRemoved;
		public override event EventHandler<ConferenceEventArgs> OnConferenceAdded;
		public override event EventHandler<ConferenceEventArgs> OnConferenceRemoved;

		#endregion

		private readonly VaddioAvBridgeAudioComponent m_AudioComponent;
		private readonly VaddioAvBridgeVideoComponent m_VideoComponent;

		/// <summary>
		/// Gets the type of conference this dialer supports.
		/// </summary>
		public override eCallType Supports { get { return eCallType.Audio | eCallType.Video; } }

		#region Constructor

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public VaddioAvBridgeConferenceControl(VaddioAvBridgeDevice parent, int id) 
			: base(parent, id)
		{
			m_AudioComponent = parent.Components.GetComponent<VaddioAvBridgeAudioComponent>();
			m_VideoComponent = parent.Components.GetComponent<VaddioAvBridgeVideoComponent>();

			Subscribe(m_AudioComponent);
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			Unsubscribe(m_AudioComponent);
		}

		#endregion

		#region Methods

		/// <summary>
		/// Gets the active conference sources.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<VaddioAvBridgeWebinar> GetConferences()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Returns the level of support the device has for the given context.
		/// </summary>
		/// <param name="dialContext"></param>
		/// <returns></returns>
		public override eDialContextSupport CanDial(IDialContext dialContext)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Dials the given context.
		/// </summary>
		/// <param name="dialContext"></param>
		public override void Dial(IDialContext dialContext)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Sets the do-not-disturb enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public override void SetDoNotDisturb(bool enabled)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Sets the auto-answer enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public override void SetAutoAnswer(bool enabled)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Sets the privacy mute enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public override void SetPrivacyMute(bool enabled)
		{
			m_AudioComponent.SetAudioMute(enabled);
		}

		#endregion

		#region Audio Component Callbacks

		private void Subscribe(VaddioAvBridgeAudioComponent audioComponent)
		{
			audioComponent.OnAudioMuteChanged += AudioComponentOnAudioMuteChanged;
		}

		private void Unsubscribe(VaddioAvBridgeAudioComponent audioComponent)
		{
			audioComponent.OnAudioMuteChanged -= AudioComponentOnAudioMuteChanged;
		}

		private void AudioComponentOnAudioMuteChanged(object sender, BoolEventArgs boolEventArgs)
		{
			PrivacyMuted = boolEventArgs.Data;
		}

		#endregion
	}
}