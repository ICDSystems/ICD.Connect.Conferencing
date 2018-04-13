using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.Controls;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Devices;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Network.Attributes.Rpc;
using ICD.Connect.Protocol.Network.RemoteProcedure;
using ICD.Connect.Protocol.Network.Tcp;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Conferencing.Server
{
	[PublicAPI]
	public sealed class ConferencingServerDevice : AbstractDevice<ConferencingServerDeviceSettings>
	{
		#region RPC Constants

		public const string DIAL_RPC = "Dial";
		public const string DIAL_TYPE_RPC = "DialType";

		public const string PRIVACY_MUTE_RPC = "PrivacyMute";
		public const string AUTO_ANSWER_RPC = "AutoAnswer";
		public const string DO_NOT_DISTURB_RPC = "DoNotDisturb";

		public const string ANSWER_RPC = "Answer";

		public const string HOLD_ENABLE_RPC = "HoldEnable";
		public const string HOLD_RESUME_RPC = "HoldResume";

		public const string END_CALL_RPC = "EndCall";

		public const string SEND_DTMF_RPC = "SendDTMF";

		#endregion

		#region Private Members
		private IDialingDeviceControl m_WrappedControl;
		private AsyncTcpServer m_Server;
		private readonly ServerSerialRpcController m_RpcController;

		private readonly Dictionary<Guid, IConferenceSource> m_Sources;
		#endregion

		public ConferencingServerDevice()
		{
			m_RpcController = new ServerSerialRpcController(this);
			m_Sources = new Dictionary<Guid, IConferenceSource>();
		}

		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);
			SetControl(null);
			ClearSources();
		}

		#region Private Helper Methods

		private void CallActionForConnectedClients(Action<uint> action)
		{
			if (m_Server == null)
				return;

			foreach (uint client in m_Server.GetClients())
			{
				action(client);
			}
		}

		/// <summary>
		/// Logs to logging core.
		/// </summary>
		/// <param name="severity"></param>
		/// <param name="message"></param>
		/// <param name="args"></param>
		private void Log(eSeverity severity, string message, params object[] args)
		{
			message = string.Format(message, args);
			message = string.Format("{0} - {1}", GetType().Name, message);

			Logger.AddEntry(severity, message);
		}

		#endregion

		#region RPCs

		[Rpc(DIAL_RPC), UsedImplicitly]
		public void Dial(string number)
		{
			if (m_WrappedControl == null)
				return;
			m_WrappedControl.Dial(number);
		}

		[Rpc(DIAL_TYPE_RPC), UsedImplicitly]
		public void Dial(string number, eConferenceSourceType type)
		{
			if (m_WrappedControl == null)
				return;
			m_WrappedControl.Dial(number, type);
		}

		[Rpc(PRIVACY_MUTE_RPC), UsedImplicitly]
		public void PrivacyMute(bool enabled)
		{
			if (m_WrappedControl == null)
				return;
			m_WrappedControl.SetPrivacyMute(enabled);
		}

		[Rpc(AUTO_ANSWER_RPC), UsedImplicitly]
		public void SetAutoAnswer(bool enabled)
		{
			if (m_WrappedControl == null)
				return;
			m_WrappedControl.SetAutoAnswer(enabled);
		}

		[Rpc(DO_NOT_DISTURB_RPC), UsedImplicitly]
		public void SetDoNotDisturb(bool enabled)
		{
			if (m_WrappedControl == null)
				return;
			m_WrappedControl.SetDoNotDisturb(enabled);
		}

		[Rpc(HOLD_ENABLE_RPC), UsedImplicitly]
		public void HoldEnable(Guid id)
		{
			if (m_WrappedControl == null)
				return;
			if (!m_Sources.ContainsKey(id))
			{
				Log(eSeverity.Error, "No Source with the given key found.");
				return;
			}

			var source = m_Sources[id];
			source.Hold();
		}

		[Rpc(ANSWER_RPC), UsedImplicitly]
		public void Answer(Guid id)
		{
			if (m_WrappedControl == null)
				return;

			if (!m_Sources.ContainsKey(id))
			{
				Log(eSeverity.Error, "No Source with the given key found.");
				return;
			}

			var source = m_Sources[id];
			source.Answer();
		}

		[Rpc(HOLD_RESUME_RPC), UsedImplicitly]
		public void HoldResume(Guid id)
		{
			if (m_WrappedControl == null)
				return;

			if (!m_Sources.ContainsKey(id))
			{
				Log(eSeverity.Error, "No Source with the given key found.");
				return;
			}

			var source = m_Sources[id];
			source.Resume();
		}

		[Rpc(END_CALL_RPC), UsedImplicitly]
		public void EndCall(Guid id)
		{
			if (m_WrappedControl == null)
				return;

			if (!m_Sources.ContainsKey(id))
			{
				Log(eSeverity.Error, "No Source with the given key found.");
				return;
			}

			var source = m_Sources[id];
			source.Hangup();
		}

		[Rpc(SEND_DTMF_RPC), UsedImplicitly]
		public void SendDtmf(Guid id, string data)
		{
			if (m_WrappedControl == null)
				return;

			if (!m_Sources.ContainsKey(id))
			{
				Log(eSeverity.Error, "No Source with the given key found.");
				return;
			}

			var source = m_Sources[id];
			source.SendDtmf(data);
		}
		#endregion

		#region Control
		/// <summary>
		/// Sets the control for this server to wrap.
		/// </summary>
		/// <param name="control"></param>
		[PublicAPI]
		public void SetControl(IDialingDeviceControl control)
		{
			if (m_WrappedControl == control)
				return;

			ClearSources();

			Unsubscribe(control);

			m_WrappedControl = control;

			Subscribe(control);

			if (m_WrappedControl == null)
				return;

			foreach (var source in m_WrappedControl.GetSources())
			{
				AddSource(source);
			}
		}

		private void Subscribe(IDialingDeviceControl control)
		{
			control.OnPrivacyMuteChanged += WrappedOnPrivacyMuteChanged;
			control.OnSourceAdded += WrappedOnSourceAdded;
		}

		private void Unsubscribe(IDialingDeviceControl control)
		{
			control.OnPrivacyMuteChanged -= WrappedOnPrivacyMuteChanged;
			control.OnSourceAdded -= WrappedOnSourceAdded;
		}

		private void WrappedOnPrivacyMuteChanged(object sender, BoolEventArgs args)
		{
			const string key = ConferencingClientDevice.SET_CACHED_PRIVACY_MUTE_STATE;
			CallActionForConnectedClients(clientId => m_RpcController.CallMethod(clientId, key, args.Data));
		}

		private void WrappedOnSourceAdded(object sender, ConferenceSourceEventArgs args)
		{
			AddSource(args.Data);
		}
		#endregion

		#region Sources

		private void AddSource(IConferenceSource source)
		{
			if (m_Sources.ContainsValue(source))
				return;

			m_Sources.Add(Guid.NewGuid(), source);

			Subscribe(source);
		}

		private void ClearSources()
		{
			foreach (var source in m_Sources.Values)
			{
				Unsubscribe(source);
			}

			m_Sources.Clear();
		}

		private void Subscribe(IConferenceSource source)
		{
			source.OnStatusChanged += SourceOnPropertyChanged;
			source.OnAnswerStateChanged += SourceOnPropertyChanged;
			source.OnNameChanged += SourceOnPropertyChanged;
			source.OnNumberChanged += SourceOnPropertyChanged;
			source.OnSourceTypeChanged += SourceOnPropertyChanged;
		}

		private void Unsubscribe(IConferenceSource source)
		{
			source.OnStatusChanged -= SourceOnPropertyChanged;
			source.OnAnswerStateChanged -= SourceOnPropertyChanged;
			source.OnNameChanged -= SourceOnPropertyChanged;
			source.OnNumberChanged -= SourceOnPropertyChanged;
			source.OnSourceTypeChanged -= SourceOnPropertyChanged;
		}

		private void SourceOnPropertyChanged(object sender, EventArgs args)
		{
			var source = sender as IConferenceSource;
			if (source == null)
				return;

			if (!m_Sources.ContainsValue(source))
			{
				Log(eSeverity.Error, "Unknown source {0}", source.Name);
				return;
			}

			var id = m_Sources.GetKey(source);
			var rpcSource = new RpcConferenceSource(source);
			const string key = ConferencingClientDevice.UPDATE_CACHED_SOURCE_STATE;
			CallActionForConnectedClients(clientId => m_RpcController.CallMethod(clientId, key, id, rpcSource));
		}
		#endregion

		#region TCP Server

		/// <summary>
		/// Sets the server for communication with clients.
		/// </summary>
		/// <param name="server"></param>
		[PublicAPI]
		public void SetServer(AsyncTcpServer server)
		{
			if (server == m_Server)
				return;

			Unsubscribe(m_Server);
			m_Server = server;
			m_RpcController.SetServer(m_Server);
			Subscribe(m_Server);
		}

		private void Subscribe(AsyncTcpServer server)
		{
			if (server != null)
				server.OnSocketStateChange += ServerOnSocketStateChange;
		}

		private void Unsubscribe(AsyncTcpServer server)
		{
			if (server != null)
				server.OnSocketStateChange -= ServerOnSocketStateChange;
		}

		private void ServerOnSocketStateChange(object sender, SocketStateEventArgs args)
		{
			if (args.SocketState == SocketStateEventArgs.eSocketStatus.SocketStatusConnected)
				return;
		}
		#endregion

		#region Settings

		protected override void ApplySettingsFinal(ConferencingServerDeviceSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			if (settings.WrappedDeviceId == null)
			{
				Logger.AddEntry(eSeverity.Error, "No Wrapped Device Id Found");
				return;
			}

			var wrappedDevice = factory.GetOriginatorById<IDevice>(settings.WrappedDeviceId.Value);
			if (wrappedDevice == null)
			{
				Logger.AddEntry(eSeverity.Error, "No device found with Id:" + settings.WrappedDeviceId.Value);
				return;
			}

			if (settings.WrappedControlId != null)
			{
				if (!wrappedDevice.Controls.Contains(settings.WrappedControlId.Value))
				{
					Logger.AddEntry(eSeverity.Error, "No control found with Id:{0} on device {1}.", settings.WrappedControlId,
									settings.WrappedDeviceId);
					return;
				}
				IDialingDeviceControl control = wrappedDevice.Controls.GetControl(settings.WrappedControlId.Value) as IDialingDeviceControl;
				if (control == null)
				{
					Logger.AddEntry(eSeverity.Error, "Control Id:{0} on device {1} is not of type IDialingDeviceControl",
									settings.WrappedControlId, settings.WrappedDeviceId);
					return;
				}
				SetControl(control);
			}
			else
			{
				IDialingDeviceControl control = wrappedDevice.Controls.GetControl<IDialingDeviceControl>();
				if (control == null)
				{
					Logger.AddEntry(eSeverity.Error, "No control of type IDialingDeviceControl found on device {0}", settings.WrappedDeviceId);
					return;
				}
				SetControl(control);
			}
		}

		protected override void CopySettingsFinal(ConferencingServerDeviceSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.WrappedDeviceId = m_WrappedControl == null ? null : m_WrappedControl.Parent == null ? (int?)null : m_WrappedControl.Parent.Id;
			settings.WrappedControlId = m_WrappedControl == null ? (int?)null : m_WrappedControl.Id;
		}

		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			SetControl(null);
		}

		#endregion

		#region IDevice
		protected override bool GetIsOnlineStatus()
		{
			return m_WrappedControl != null && m_WrappedControl.Parent != null && m_WrappedControl.Parent.IsOnline;
		}
		#endregion
	}
}