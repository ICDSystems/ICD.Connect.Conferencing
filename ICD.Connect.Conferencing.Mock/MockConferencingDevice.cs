using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.Devices;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.IncomingCalls;
using ICD.Connect.Conferencing.Participants;
using ICD.Connect.Conferencing.Participants.Enums;
using ICD.Connect.Devices;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Devices.Mock;
using ICD.Connect.Settings;

namespace ICD.Connect.Conferencing.Mock
{
	public sealed class MockConferencingDevice : AbstractMockDevice<MockConferencingDeviceSettings>, IMockConferencingDevice
	{
		public event EventHandler<GenericEventArgs<IParticipant>> OnParticipantAdded;
		public event EventHandler<GenericEventArgs<IParticipant>> OnParticipantRemoved;
		public event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallAdded;
		public event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallRemoved;

		#region Private Memebers

		private readonly List<IParticipant> m_Sources;
		private readonly CodecInputTypes m_InputTypes;

		#endregion

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
		public MockConferencingDevice()
		{
			m_Sources = new List<IParticipant>();

			m_InputTypes = new CodecInputTypes();
			m_InputTypes.SetInputType(1, eCodecInputType.Camera);
			m_InputTypes.SetInputType(2, eCodecInputType.Content);
			m_InputTypes.SetInputType(3, eCodecInputType.None);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			OnParticipantAdded = null;
			OnParticipantRemoved = null;

			base.DisposeFinal(disposing);

			m_Sources.Clear();
		}

		/// <summary>
		/// Override to add controls to the device.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		/// <param name="addControl"></param>
		protected override void AddControls(MockConferencingDeviceSettings settings, IDeviceFactory factory, Action<IDeviceControl> addControl)
		{
			base.AddControls(settings, factory, addControl);

			addControl(new MockVideoConferenceRouteControl(this, 0));
			addControl(new MockTraditionalConferenceDeviceControl(this, 1));
			addControl(new MockDirectoryControl(this, 2));
		}

		#region Console

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			yield return ConsoleNodeGroup.IndexNodeMap("Sources", "", GetSources().OfType<IConsoleNodeBase>());
		}

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
		{
			return base.GetConsoleNodes();
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new ConsoleCommand("MockIncomingCall", "Generates a mock incoming call", () => MockIncomingCall());
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion

		public void SetInputTypeForInput(int address, eCodecInputType type)
		{
			m_InputTypes.SetInputType(address, type);
		}

		public IEnumerable<IParticipant> GetSources()
		{
			return m_Sources.ToArray(m_Sources.Count);
		}

		private void Dial(string number, eCallType type)
		{
			ThinParticipant participant =
				new ThinParticipant
				{
					HangupCallback = HangupCallback
				};

			participant.SetDialTime(IcdEnvironment.GetUtcTime());
			participant.SetDirection(eCallDirection.Outgoing);
			participant.SetNumber(number);
			participant.SetName("Mock Call To: " + number);
			participant.SetStatus(eParticipantStatus.Connected);
			participant.SetCallType(type);

			m_Sources.Add(participant);

			OnParticipantAdded.Raise(this, new GenericEventArgs<IParticipant>(participant));
		}

		private void HangupCallback(ThinParticipant sender)
		{
			m_Sources.Remove(sender);

			OnParticipantRemoved.Raise(this, new GenericEventArgs<IParticipant>(sender));
		}

		public eDialContextSupport CanDial(IDialContext dialContext)
		{
			return eDialContextSupport.Supported;
		}

		public void Dial(IDialContext dialContext)
		{
			Dial(dialContext.DialString, dialContext.CallType);
		}

		public void StartPersonalMeeting()
		{
			var context = new DialContext
			{
				CallType = eCallType.Audio | eCallType.Video,
				DialString = "Mock Personal Meeting"
			};

			Dial(context);
		}

		private void MockIncomingCall()
		{
// ReSharper disable once PossibleNullReferenceException
			var incomingCall = new TraditionalIncomingCall(Controls.GetControl<IConferenceDeviceControl>().Supports);
			OnIncomingCallAdded.Raise(this, new GenericEventArgs<IIncomingCall>(incomingCall));
		}
	}
}
