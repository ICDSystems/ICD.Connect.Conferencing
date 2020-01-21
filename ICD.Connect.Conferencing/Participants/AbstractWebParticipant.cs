using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Conferencing.Participants
{
	public abstract class AbstractWebParticipant : AbstractParticipant, IWebParticipant
	{
		#region Events

		/// <summary>
		/// Raised when the participant's mute state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnIsMutedChanged;

		/// <summary>
		/// Raised when the participant's host state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnIsHostChanged;

		#endregion

		private bool m_IsMuted;
		private bool m_IsHost;
		private bool m_IsSelf;

		#region Properties

		public bool IsMuted
		{
			get { return m_IsMuted; }
			protected set
			{
				if (m_IsMuted == value)
					return;

				m_IsMuted = value;
				Log(eSeverity.Informational, "IsMuted set to {0}", m_IsMuted);
				OnIsMutedChanged.Raise(this, new BoolEventArgs(m_IsMuted));
			}
		}

		public bool IsHost
		{
			get { return m_IsHost; }
			protected set
			{
				if (m_IsHost == value)
					return;

				m_IsHost = value;
				Log(eSeverity.Informational, "IsHost set to {0}", m_IsHost);
				OnIsHostChanged.Raise(this, new BoolEventArgs(m_IsHost));
			}
		}
		
		public bool IsSelf
		{ 
			get { return m_IsSelf; }
			protected set
			{
				if (m_IsSelf == value)
					return;

				m_IsSelf = value;
				
				Log(eSeverity.Informational, "IsSelf set to {0}", m_IsSelf);
			} 
		}

		#endregion

		#region Methods

		public abstract void Kick();

		public abstract void Mute(bool mute);

		protected override void DisposeFinal()
		{
			OnIsMutedChanged = null;
			OnIsHostChanged = null;

			base.DisposeFinal();
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

			addRow("Is Muted", IsMuted);
			addRow("Is Self", IsSelf);
			addRow("Is Host", IsHost);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new ConsoleCommand("Kick", "Kicks the participant", () => Kick());
			yield return new GenericConsoleCommand<bool>("Mute", "Usage: Mute <true/false>", m => Mute(m));
		}

		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
