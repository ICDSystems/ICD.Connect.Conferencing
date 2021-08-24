using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Logging.Activities;
using ICD.Common.Logging.LoggingContexts;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.IncomingCalls;
using ICD.Connect.Conferencing.Server.Conferences;
using ICD.Connect.Conferencing.Server.Devices.Server;
using ICD.Connect.Conferencing.Utils;
using ICD.Connect.Devices;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Network.Attributes.Rpc;
using ICD.Connect.Protocol.Network.Ports;
using ICD.Connect.Protocol.Network.RemoteProcedure;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings;

namespace ICD.Connect.Conferencing.Server.Devices.Client
{
	public sealed class InterpretationClientDevice : AbstractDevice<InterpretationClientDeviceSettings>,
													 IClientInterpretationDevice
	{
		#region Events

		public event EventHandler OnInterpretationActiveChanged;

		public event EventHandler<ConferenceEventArgs> OnConferenceAdded;
		public event EventHandler<ConferenceEventArgs> OnConferenceRemoved;
		public event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallAdded;
		public event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallRemoved;

		public event EventHandler<BoolEventArgs> OnDoNotDisturbChanged;
		public event EventHandler<BoolEventArgs> OnAutoAnswerChanged;
		public event EventHandler<BoolEventArgs> OnPrivacyMuteChanged;

		#endregion

		#region RPC Constants

		public const string SET_INTERPRETATION_STATE_RPC = "SetInterpretationState";

		public const string SET_CACHED_PRIVACY_MUTE_STATE = "SetCachedPrivacyMuteState";
		public const string SET_CACHED_AUTO_ANSWER_STATE = "SetCachedAutoAnswerState";
		public const string SET_CACHED_DO_NOT_DISTURB_STATE = "SetCachedDoNotDisturbState";

		public const string UPDATE_CACHED_CONFERENCE_STATE = "UpdateCachedConferenceState";
		public const string REMOVE_CACHED_CONFERENCE = "RemoveCachedConference";

		#endregion

		#region Private Members

		private readonly SecureNetworkProperties m_NetworkProperties;

		private readonly ClientSerialRpcController m_RpcController;
		private readonly BiDictionary<Guid, InterpretationThinConference> m_Conferences;
		private readonly SafeCriticalSection m_SourcesCriticalSection;

		private bool m_IsConnected;
		private bool m_IsInterpretationActive;
		private bool m_PrivacyMuted;
		private bool m_DoNotDisturb;
		private bool m_AutoAnswer;
		private int m_RoomId;

		#endregion

		#region Public Properties

		public bool IsConnected
		{
			get { return m_IsConnected; }
			private set
			{
				try
				{
					if (value == m_IsConnected)
						return;

					m_IsConnected = value;

					Logger.LogSetTo(m_IsConnected ? eSeverity.Informational : eSeverity.Error, "IsConnected", m_IsConnected);

					UpdateCachedOnlineStatus();

					if (m_IsConnected)
						Register();
				}
				finally
				{
					Activities.LogActivity(m_IsConnected
						? new Activity(Activity.ePriority.Low, "Connected", "Connected To Server", eSeverity.Informational)
						: new Activity(Activity.ePriority.High, "Connected", "Not Connected To Server", eSeverity.Error));
				}
			}
		}

		public string RoomName { get; private set; }

		public bool IsInterpretationActive
		{
			get { return m_IsInterpretationActive; }
			private set
			{
				if (m_IsInterpretationActive == value)
					return;

				m_IsInterpretationActive = value;

				OnInterpretationActiveChanged.Raise(this);
			}
		}

		public bool PrivacyMuted
		{
			get { return m_PrivacyMuted; }
			private set
			{
				if (value == m_PrivacyMuted)
					return;

				m_PrivacyMuted = value;

				OnPrivacyMuteChanged.Raise(this, new BoolEventArgs(m_PrivacyMuted));
			}
		}

		public bool DoNotDisturb
		{
			get { return m_DoNotDisturb; }
			private set
			{
				if (value == m_DoNotDisturb)
					return;

				m_DoNotDisturb = value;

				OnDoNotDisturbChanged.Raise(this, new BoolEventArgs(m_DoNotDisturb));
			}
		}

		public bool AutoAnswer
		{
			get { return m_AutoAnswer; }
			private set
			{
				if (value == m_AutoAnswer)
					return;

				m_AutoAnswer = value;

				OnAutoAnswerChanged.Raise(this, new BoolEventArgs(m_AutoAnswer));
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public InterpretationClientDevice()
		{
			m_NetworkProperties = new SecureNetworkProperties();
			m_RpcController = new ClientSerialRpcController(this);
			m_Conferences = new BiDictionary<Guid, InterpretationThinConference>();
			m_SourcesCriticalSection = new SafeCriticalSection();

			m_RpcController.OnIsOnlineStateChanged += PortOnIsOnlineStateChanged;
			m_RpcController.OnConnectedStateChanged += PortOnConnectedStateChanged;

			// Initialize activities
			IsConnected = false;
		}

		protected override void DisposeFinal(bool disposing)
		{
			OnInterpretationActiveChanged = null;
			OnConferenceAdded = null;
			OnConferenceRemoved = null;
			OnIncomingCallAdded = null;
			OnIncomingCallRemoved = null;
			OnDoNotDisturbChanged = null;
			OnAutoAnswerChanged = null;
			OnPrivacyMuteChanged = null;

			base.DisposeFinal(disposing);

			m_RpcController.OnIsOnlineStateChanged -= PortOnIsOnlineStateChanged;
			m_RpcController.OnConnectedStateChanged -= PortOnConnectedStateChanged;
			m_RpcController.Dispose();
		}

		#region	Public Methods

		public void SetRoomNameIfNullOrEmpty(string name)
		{
			// Only allow the room name to be set externally if an override isn't provided in settings.
			if (string.IsNullOrEmpty(RoomName))
				RoomName = name;
		}

		public void Register()
		{
			if (IsConnected)
				m_RpcController.CallMethod(InterpretationServerDevice.REGISTER_ROOM_RPC, m_RoomId, RoomName);
		}

		public void Unregister()
		{
			if (IsConnected)
				m_RpcController.CallMethod(InterpretationServerDevice.UNREGISTER_ROOM_RPC, m_RoomId);
		}

		public void Dial(string number)
		{
			if (IsConnected)
				m_RpcController.CallMethod(InterpretationServerDevice.DIAL_RPC, m_RoomId, number);
		}

		public void Dial(string number, eCallType callType)
		{
			if (IsConnected)
				m_RpcController.CallMethod(InterpretationServerDevice.DIAL_TYPE_RPC, m_RoomId, number, callType);
		}

		/// <summary>
		/// Returns the level of support the dialer has for the given booking.
		/// </summary>
		/// <param name="dialContext"></param>
		/// <returns></returns>
		public eDialContextSupport CanDial(IDialContext dialContext)
		{
			if (string.IsNullOrEmpty(dialContext.DialString))
				return eDialContextSupport.Unsupported;

			if (dialContext.Protocol == eDialProtocol.Sip && SipUtils.IsValidSipUri(dialContext.DialString))
				return eDialContextSupport.Supported;

			if (dialContext.Protocol == eDialProtocol.Pstn)
				return eDialContextSupport.Supported;

			return eDialContextSupport.Unsupported;
		}

		/// <summary>
		/// Dials the given booking.
		/// </summary>
		/// <param name="dialContext"></param>
		public void Dial(IDialContext dialContext)
		{
			Dial(dialContext.DialString);
		}

		public void SetPrivacyMute(bool enabled)
		{
			if (IsConnected)
				m_RpcController.CallMethod(InterpretationServerDevice.PRIVACY_MUTE_RPC, m_RoomId, enabled);
		}

		public void SetAutoAnswer(bool enabled)
		{
			if (IsConnected)
				m_RpcController.CallMethod(InterpretationServerDevice.AUTO_ANSWER_RPC, m_RoomId, enabled);
		}

		public void SetDoNotDisturb(bool enabled)
		{
			if (IsConnected)
				m_RpcController.CallMethod(InterpretationServerDevice.DO_NOT_DISTURB_RPC, m_RoomId, enabled);
		}

		[PublicAPI]
		public IEnumerable<IConference> GetConferences()
		{
			return m_SourcesCriticalSection.Execute(() => m_Conferences.Values.ToArray(m_Conferences.Count));
		}

		#endregion

		#region Private Helper Methods

		private void ClearParticipants()
		{
			m_SourcesCriticalSection.Enter();
			try
			{
				foreach (var conference in m_Conferences.Values)
				{
					conference.Status = (eConferenceStatus.Disconnected);
					Unsubscribe(conference);

					OnConferenceRemoved.Raise(this, conference);
				}

				m_Conferences.Clear();
			}
			finally
			{
				m_SourcesCriticalSection.Leave();
			}
		}

		#endregion

		#region RPCs

		[Rpc(SET_INTERPRETATION_STATE_RPC), UsedImplicitly]
		private void SetInterpretationState(bool state)
		{
			IsInterpretationActive = state;

			ClearParticipants();
		}

		[Rpc(SET_CACHED_PRIVACY_MUTE_STATE), UsedImplicitly]
		private void SetCachedPrivacyMuteState(bool state)
		{
			PrivacyMuted = state;
		}

		[Rpc(SET_CACHED_AUTO_ANSWER_STATE), UsedImplicitly]
		private void SetCachedAutoAnswerState(bool state)
		{
			AutoAnswer = state;
		}

		[Rpc(SET_CACHED_DO_NOT_DISTURB_STATE), UsedImplicitly]
		private void SetCachedDoNotDisturbState(bool state)
		{
			DoNotDisturb = state;
		}

		[Rpc(REMOVE_CACHED_CONFERENCE), UsedImplicitly]
		private void RemoveCachedConference(Guid id)
		{
			m_SourcesCriticalSection.Enter();

			try
			{
				if (!m_Conferences.ContainsKey(id))
					return;

				var sourceToRemove = m_Conferences.GetValue(id);
				sourceToRemove.Status = eConferenceStatus.Disconnected;
				Unsubscribe(sourceToRemove);
				m_Conferences.RemoveKey(id);

				OnConferenceRemoved.Raise(this, sourceToRemove);
			}
			finally
			{
				m_SourcesCriticalSection.Leave();
			}
		}

		[Rpc(UPDATE_CACHED_CONFERENCE_STATE), UsedImplicitly]
		private void UpdateCachedConferenceState(Guid id, ConferenceState conferenceState)
		{
			InterpretationThinConference added = null;
			InterpretationThinConference removed = null;

			m_SourcesCriticalSection.Enter();

			try
			{
				InterpretationThinConference conference;

				if (!m_Conferences.TryGetValue(id, out conference))
				{
					conference = InterpretationThinConference.FromConferenceState(conferenceState);
					m_Conferences.Set(id, conference);
					Subscribe(conference);

					added = conference;
				}
				else
				{
					conference.UpdateFromConferenceState(conferenceState);
				}

				if (conference.Status == eConferenceStatus.Disconnected)
				{
					removed = conference;
					Unsubscribe(removed);
					m_Conferences.RemoveKey(id);
				}
			}
			finally
			{
				m_SourcesCriticalSection.Leave();
			}

			if (added != null)
				OnConferenceAdded.Raise(this, added);

			if (removed != null)
				OnConferenceRemoved.Raise(this, removed);

		}

		#endregion

		#region Conferences

		private bool TryGetId(InterpretationThinConference conference, out Guid id)
		{
			m_SourcesCriticalSection.Enter();

			try
			{
				return m_Conferences.TryGetKey(conference, out id);
			}
			finally
			{
				m_SourcesCriticalSection.Leave();
			}
		}

		private void Subscribe(InterpretationThinConference conference)
		{
			conference.HoldCallback += ConferenceHoldCallCallback;
			conference.ResumeCallback += ConferenceResumeCallCallback;
			conference.SendDtmfCallback += ConferenceSendDtmfCallback;
			conference.LeaveConferenceCallback += ConferenceLeaveConferenceCallback;
			conference.EndConferenceCallback += ConferenceEndConferenceCallback;
			conference.StartRecordingCallback += ConferenceStartRecordingCallback;
			conference.StopRecordingCallback += ConferenceStopRecordingCallback;
			conference.PauseRecordingCallback += ConferencePauseRecordingCallback;
		}

		private void Unsubscribe(InterpretationThinConference conference)
		{
			conference.HoldCallback = null;
			conference.ResumeCallback = null;
			conference.SendDtmfCallback = null;
			conference.LeaveConferenceCallback = null;
			conference.EndConferenceCallback = null;
			conference.StartRecordingCallback = null;
			conference.StopRecordingCallback = null;
			conference.PauseRecordingCallback = null;
		}

		private void ConferenceHoldCallCallback(InterpretationThinConference conference)
		{
			if (conference == null)
				return;

			Guid id;
			if (!TryGetId(conference, out id))
				return;

			if (IsConnected)
				m_RpcController.CallMethod(InterpretationServerDevice.HOLD_ENABLE_RPC, m_RoomId, id);
		}

		private void ConferenceResumeCallCallback(InterpretationThinConference conference)
		{
			if (conference == null)
				return;

			Guid id;
			if (!TryGetId(conference, out id))
				return;

			if (IsConnected)
				m_RpcController.CallMethod(InterpretationServerDevice.HOLD_RESUME_RPC, m_RoomId, id);
		}

		private void ConferenceSendDtmfCallback(InterpretationThinConference conference, string data)
		{
			if (conference == null)
				return;

			Guid id;
			if (!TryGetId(conference, out id))
				return;

			if (IsConnected)
				m_RpcController.CallMethod(InterpretationServerDevice.SEND_DTMF_RPC, m_RoomId, id, data);
		}

		private void ConferenceLeaveConferenceCallback(InterpretationThinConference conference)
		{
			if (conference == null)
				return;

			Guid id;
			if (!TryGetId(conference, out id))
				return;

			if (IsConnected)
				m_RpcController.CallMethod(InterpretationServerDevice.LEAVE_CONFERENCE_RPC, m_RoomId, id);
		}

		private void ConferenceEndConferenceCallback(InterpretationThinConference conference)
		{
			if (conference == null)
				return;

			Guid id;
			if (!TryGetId(conference, out id))
				return;

			if (IsConnected)
				m_RpcController.CallMethod(InterpretationServerDevice.END_CONFERENCE_RPC, m_RoomId, id);
		}

		private void ConferenceStartRecordingCallback(InterpretationThinConference conference)
		{
			if (conference == null)
				return;

			Guid id;
			if (!TryGetId(conference, out id))
				return;

			if (IsConnected)
				m_RpcController.CallMethod(InterpretationServerDevice.START_RECORDING_RPC, m_RoomId, id);
		}

		private void ConferenceStopRecordingCallback(InterpretationThinConference conference)
		{
			if (conference == null)
				return;

			Guid id;
			if (!TryGetId(conference, out id))
				return;

			if (IsConnected)
				m_RpcController.CallMethod(InterpretationServerDevice.STOP_RECORDING_RPC, m_RoomId, id);
		}

		private void ConferencePauseRecordingCallback(InterpretationThinConference conference)
		{
			if (conference == null)
				return;

			Guid id;
			if (!TryGetId(conference, out id))
				return;

			if (IsConnected)
				m_RpcController.CallMethod(InterpretationServerDevice.PAUSE_RECORDING_RPC, m_RoomId, id);
		}


		

		#endregion

		#region Port

		[PublicAPI]
		public void SetPort(ISerialPort port)
		{
			m_RpcController.SetPort(port, false);

			UpdateCachedOnlineStatus();

			if (m_RpcController.IsConnected)
				Register();
		}

		/// <summary>
		/// Sets the port for communication with the server.
		/// </summary>
		/// <param name="port"></param>
		[PublicAPI]
		public void ConfigurePort(ISerialPort port)
		{
			// Network (TCP, UDP, SSH)
			if (port is ISecureNetworkPort)
				(port as ISecureNetworkPort).ApplyDeviceConfiguration(m_NetworkProperties);
			else if (port is INetworkPort)
				(port as INetworkPort).ApplyDeviceConfiguration(m_NetworkProperties);
		}

		/// <summary>
		/// Called when the port online status changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void PortOnIsOnlineStateChanged(object sender, BoolEventArgs args)
		{
			UpdateCachedOnlineStatus();
		}

		/// <summary>
		/// Called when the port connection state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void PortOnConnectedStateChanged(object sender, BoolEventArgs args)
		{
			IsConnected = m_RpcController != null && m_RpcController.IsConnected;
			if (IsConnected)
				return;

			IsInterpretationActive = false;
			ClearParticipants();
		}

		#endregion

		#region Settings

		protected override void ApplySettingsFinal(InterpretationClientDeviceSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			m_RoomId = settings.Room == null ? 0 : settings.Room.Value;

			RoomName = settings.RoomName;

			m_NetworkProperties.Copy(settings);

			ISerialPort port = null;

			if (settings.Port != null)
			{
				try
				{
					port = factory.GetPortById(settings.Port.Value) as ISerialPort;
				}
				catch (KeyNotFoundException)
				{
					Logger.Log(eSeverity.Error, "No Serial Port with id {0}", settings.Port);
				}
			}

			//todo: Dejank?
			ConfigurePort(port);

			SetPort(port);
		}

		protected override void CopySettingsFinal(InterpretationClientDeviceSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Port = m_RpcController.PortNumber;
			settings.Room = m_RoomId;
			settings.RoomName = RoomName;

			settings.Copy(m_NetworkProperties);
		}

		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			m_NetworkProperties.ClearNetworkProperties();

			SetPort(null);
		}

		/// <summary>
		/// Override to add controls to the device.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		/// <param name="addControl"></param>
		protected override void AddControls(InterpretationClientDeviceSettings settings, IDeviceFactory factory,
		                                    Action<IDeviceControl> addControl)
		{
			base.AddControls(settings, factory, addControl);

			addControl(new DialerDeviceDialerControl(this, 0));
		}

		/// <summary>
		/// Override to add actions on StartSettings
		/// This should be used to start communications with devices and perform initial actions
		/// </summary>
		protected override void StartSettingsFinal()
		{
			base.StartSettingsFinal();

			m_RpcController.Start();
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

			var sources = GetConferences();
			addRow("Interpretation Active", m_IsInterpretationActive);
			addRow("Remote Sources", "Count: " + sources.Count());
			foreach (var src in GetConferences())
			{
				addRow("-----", "-----");
				addRow("Name", src.Name);
				//addRow("Number", src.Number);
				addRow("Status", src.Status);
				addRow("Start", src.StartTime);
			}
			addRow("-----", "-----");
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsolCommands())
				yield return command;

			yield return new ConsoleCommand("Start", "Connect to the server", () => m_RpcController.Start());
			yield return new ConsoleCommand("Stop", "Disconnect from the server", () => m_RpcController.Stop());
			yield return new ConsoleCommand("Register", "Register the room with the server", () => Register());
			yield return new ConsoleCommand("Unregister", "Unregister the room with the server", () => Unregister());
		}

		private IEnumerable<IConsoleCommand> GetBaseConsolCommands()
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

			if (m_RpcController != null)
				yield return m_RpcController.Port;
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

		#region IDevice

		protected override bool GetIsOnlineStatus()
		{
			return m_RpcController != null && m_RpcController.IsOnline;
		}

		#endregion
	}
}
