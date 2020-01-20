using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.IncomingCalls;
using ICD.Connect.Conferencing.Participants;
using ICD.Connect.Devices;

namespace ICD.Connect.Conferencing.Mock
{
	public sealed class MockConferencingDevice : AbstractDevice<MockConferencingDeviceSettings>, IMockConferencingDevice
	{
		public event EventHandler<GenericEventArgs<ITraditionalParticipant>> OnParticipantAdded;
		public event EventHandler<GenericEventArgs<ITraditionalParticipant>> OnParticipantRemoved;
		public event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallAdded;
		public event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallRemoved;

		#region Private Memebers

		private bool m_Online;
		private readonly List<ITraditionalParticipant> m_Sources;

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public MockConferencingDevice()
		{
			m_Sources = new List<ITraditionalParticipant>();
			m_Online = true;

			Controls.Add(new MockTraditionalConferenceDeviceControl(this, 0));
			Controls.Add(new MockDirectoryControl(this, 1));
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
				addRow("Status",source.Status);
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

			yield return new GenericConsoleCommand<bool>("SetOnline", "Sets the online state of this device", val => m_Online = val);
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

		#region IDevice

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			return m_Online;
		}

		#endregion

		public IEnumerable<ITraditionalParticipant> GetSources()
		{
			return m_Sources.ToArray(m_Sources.Count);
		}

		private void Dial(string number, eCallType type)
		{
			ITraditionalParticipant source =
				new ThinTraditionalParticipant
				{
					DialTime = DateTime.Now,
					Direction = eCallDirection.Outgoing,
					Number = number,
					Name = "Mock Call To: " + number,
					Status = eParticipantStatus.Connected,
					CallType = type,
					HangupCallback = HangupCallback
				};

			m_Sources.Add(source);

			OnParticipantAdded.Raise(this, new GenericEventArgs<ITraditionalParticipant>(source));
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
			var incomingCall = new ThinIncomingCall();
			OnIncomingCallAdded.Raise(this, new GenericEventArgs<IIncomingCall>(incomingCall));
		}
	}
}
