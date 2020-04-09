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
using ICD.Connect.Devices;
using ICD.Connect.Devices.Mock;

namespace ICD.Connect.Conferencing.Mock
{
	public sealed class MockConferencingDevice : AbstractMockDevice<MockConferencingDeviceSettings>, IMockConferencingDevice
	{
		public event EventHandler<GenericEventArgs<ITraditionalParticipant>> OnParticipantAdded;
		public event EventHandler<GenericEventArgs<ITraditionalParticipant>> OnParticipantRemoved;
		public event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallAdded;
		public event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallRemoved;

		#region Private Memebers

		private readonly List<ITraditionalParticipant> m_Sources;
		private CodecInputTypes m_InputTypes;

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
			m_Sources = new List<ITraditionalParticipant>();

			Controls.Add(new MockVideoConferenceRouteControl(this, 0));
			Controls.Add(new MockTraditionalConferenceDeviceControl(this, 1));
			Controls.Add(new MockDirectoryControl(this, 2));

			m_InputTypes = new CodecInputTypes();
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

		#region Console

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Source Count", m_Sources.Count);
			addRow("-----", "-----");
			foreach (var source in m_Sources)
			{
				addRow("Source", source.Name);
				addRow("Number", source.Number);
				addRow("DialTime", source.DialTime);
				addRow("Status", source.Status);
				addRow("-----", "-----");
			}
		}

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
			if(address > 0 && address < 4)
				m_InputTypes.SetInputType(address, type);
		}

		public IEnumerable<ITraditionalParticipant> GetSources()
		{
			return m_Sources.ToArray(m_Sources.Count);
		}

		private void Dial(string number, eCallType type)
		{
			ThinTraditionalParticipant participant =
				new ThinTraditionalParticipant
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

			OnParticipantAdded.Raise(this, new GenericEventArgs<ITraditionalParticipant>(participant));
		}

		private void HangupCallback(ThinTraditionalParticipant sender)
		{
			m_Sources.Remove(sender);

			OnParticipantRemoved.Raise(this, new GenericEventArgs<ITraditionalParticipant>(sender));
		}

		public eDialContextSupport CanDial(IDialContext dialContext)
		{
			return eDialContextSupport.Supported;
		}

		public void Dial(IDialContext dialContext)
		{
			Dial(dialContext.DialString, dialContext.CallType);
		}

		private void MockIncomingCall()
		{
// ReSharper disable once PossibleNullReferenceException
			var incomingCall = new TraditionalIncomingCall(Controls.GetControl<IConferenceDeviceControl>().Supports);
			OnIncomingCallAdded.Raise(this, new GenericEventArgs<IIncomingCall>(incomingCall));
		}
	}
}
