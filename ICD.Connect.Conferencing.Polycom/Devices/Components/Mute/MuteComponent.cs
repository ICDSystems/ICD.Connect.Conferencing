using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Conferencing.Polycom.Devices.Components.Mute
{
	public sealed class MuteComponent : AbstractPolycomComponent
	{
		/// <summary>
		/// Raised when the near privacy mute state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnMutedNearChanged;

		/// <summary>
		/// Raised when the far mute state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnMutedFarChanged;

		private bool m_MutedNear;
		private bool m_MutedFar;

		#region Properties

		/// <summary>
		/// Gets the near privacy mute state.
		/// </summary>
		public bool MutedNear
		{
			get { return m_MutedNear; }
			private set
			{
				if (value == m_MutedNear)
					return;

				m_MutedNear = value;

				Codec.Log(eSeverity.Informational, "MutedNear set to {0}", m_MutedNear);

				OnMutedNearChanged.Raise(this, new BoolEventArgs(m_MutedNear));
			}
		}

		/// <summary>
		/// Gets the far mute state.
		/// </summary>
		public bool MutedFar
		{
			get { return m_MutedFar; }
			private set
			{
				if (value == m_MutedFar)
					return;

				m_MutedFar = value;

				Codec.Log(eSeverity.Informational, "MutedFar set to {0}", m_MutedFar);

				OnMutedFarChanged.Raise(this, new BoolEventArgs(m_MutedFar));
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="codec"></param>
		public MuteComponent(PolycomGroupSeriesDevice codec)
			: base(codec)
		{
			Subscribe(Codec);

			if (Codec.Initialized)
				Initialize();
		}

		/// <summary>
		/// Called to initialize the component.
		/// </summary>
		protected override void Initialize()
		{
			base.Initialize();

			Codec.SendCommand("mute register");

			Codec.SendCommand("mute near get");
			Codec.SendCommand("mute far get");
		}

		#region Methods

		/// <summary>
		/// Enables/disables near end privacy mute.
		/// </summary>
		/// <param name="mute"></param>
		public void MuteNear(bool mute)
		{
			Codec.SendCommand("mute near {0}", mute ? "on" : "off");
			Codec.Log(eSeverity.Informational, "Setting near mute {0}", mute ? "on" : "off");
		}

		/// <summary>
		/// Enables/disables far end mute.
		/// </summary>
		/// <param name="mute"></param>
		public void MuteFar(bool mute)
		{
			Codec.SendCommand("mute far {0}", mute ? "on" : "off");
			Codec.Log(eSeverity.Informational, "Setting far mute {0}", mute ? "on" : "off");
		}

		/// <summary>
		/// Toggles near end privacy mute.
		/// </summary>
		public void ToggleMuteNear()
		{
			Codec.SendCommand("mute near toggle");
			Codec.Log(eSeverity.Informational, "Toggling near mute");
		}

		/// <summary>
		/// Toggles far end mute.
		/// </summary>
		public void ToggleMuteFar()
		{
			Codec.SendCommand("mute far toggle");
			Codec.Log(eSeverity.Informational, "Toggling far mute");
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<bool>("MuteNear", "MuteNear <true/false>", m => MuteNear(m));
			yield return new GenericConsoleCommand<bool>("MuteFar", "MuteFar <true/false>", m => MuteFar(m));
			yield return new ConsoleCommand("MuteNearToggle", "", () => ToggleMuteNear());
			yield return new ConsoleCommand("MuteFarToggle", "", () => ToggleMuteFar());
		}

		/// <summary>
		/// Shim to avoid "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("MutedNear", MutedNear);
			addRow("MutedFar", MutedFar);
		}

		#endregion
	}
}
