using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;

namespace ICD.Connect.Conferencing.Conferences
{
	public abstract class AbstractConference<T> : IConference<T> where T: class, IParticipant
	{
		public abstract event EventHandler<ConferenceStatusEventArgs> OnStatusChanged;
        public abstract event EventHandler<ParticipantEventArgs> OnParticipantAdded;
		public abstract event EventHandler<ParticipantEventArgs> OnParticipantRemoved;

		public eConferenceStatus Status { get; private set; }
		public DateTime? Start { get; private set; }
		public DateTime? End { get; private set; }

		IEnumerable<IParticipant> IConference.GetParticipants()
		{
			return GetParticipants().Cast<IParticipant>();
		}

		public abstract IEnumerable<T> GetParticipants();

		#region Console

		public virtual string ConsoleName { get { return "Conference"; } }

		public virtual string ConsoleHelp { get { return string.Empty; }  }

		public virtual  IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (var participant in GetParticipants())
				yield return participant;
		}

		public virtual void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			addRow("Status", Status);
			addRow("ParticipantCount", GetParticipants().Count());
		}

		public virtual IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield break;
		}

		#endregion
	}
}