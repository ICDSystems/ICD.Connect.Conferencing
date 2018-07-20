using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Devices;

namespace ICD.Connect.Conferencing.Mock
{
	public sealed class MockConferencingDevice : AbstractDevice<MockConferencingDeviceSettings>, IMockConferencingDevice
	{
		public event EventHandler<ConferenceSourceEventArgs> OnSourceAdded;
		public event EventHandler<ConferenceSourceEventArgs> OnSourceRemoved;

		#region Private Memebers

		private bool m_Online;
		private readonly List<IConferenceSource> m_Sources;

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public MockConferencingDevice()
		{
			m_Sources = new List<IConferenceSource>();
			m_Online = true;

			Controls.Add(new MockDialingDeviceControl(this, 0));
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			OnSourceAdded = null;
			OnSourceRemoved = null;

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

		public IEnumerable<IConferenceSource> GetSources()
		{
			return m_Sources.ToArray(m_Sources.Count);
		}

		public void Dial(string number, eConferenceSourceType type)
		{
			IConferenceSource source =
				new ThinConferenceSource
				{
					AnswerState = eConferenceSourceAnswerState.Answered,
					DialTime = DateTime.Now,
					Direction = eConferenceSourceDirection.Outgoing,
					Number = number,
					Name = "Mock Call To: " + number,
					Status = eConferenceSourceStatus.Connected,
					SourceType = type
				};

			m_Sources.Add(source);

			OnSourceAdded.Raise(this, new ConferenceSourceEventArgs(source));
		}
	}
}
