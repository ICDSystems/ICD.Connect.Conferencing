﻿using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Calendaring.Booking;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.Contacts;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Devices;
using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Conferencing.Controls.Dialing
{
	public abstract class AbstractDialingDeviceControl<T> : AbstractDeviceControl<T>, IDialingDeviceControl
		where T : IDeviceBase
	{
		/// <summary>
		/// Raised when a source is added to the dialing component.
		/// </summary>
		public abstract event EventHandler<ConferenceSourceEventArgs> OnSourceAdded;

		/// <summary>
		/// Raised when a source is removed from the dialing component.
		/// </summary>
		public abstract event EventHandler<ConferenceSourceEventArgs> OnSourceRemoved;

		/// <summary>
		/// Raised when a source property changes.
		/// </summary>
		public event EventHandler<ConferenceSourceEventArgs> OnSourceChanged;

		/// <summary>
		/// Raised when the Do Not Disturb state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnDoNotDisturbChanged;

		/// <summary>
		/// Raised when the Auto Answer state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnAutoAnswerChanged;

		/// <summary>
		/// Raised when the microphones mute state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnPrivacyMuteChanged;

		private readonly SafeCriticalSection m_StateSection;

		private bool m_AutoAnswer;
		private bool m_PrivacyMuted;
		private bool m_DoNotDisturb;

		#region Properties

		/// <summary>
		/// Gets the AutoAnswer state.
		/// </summary>
		[PublicAPI]
		public bool AutoAnswer
		{
			get { return m_StateSection.Execute(() => m_AutoAnswer); }
			protected set
			{
				m_StateSection.Enter();

				try
				{
					if (value == m_AutoAnswer)
						return;

					m_AutoAnswer = value;

					Log(eSeverity.Informational, "AutoAnswer set to {0}", m_AutoAnswer);
				}
				finally
				{
					m_StateSection.Leave();
				}

				OnAutoAnswerChanged.Raise(this, new BoolEventArgs(m_AutoAnswer));
			}
		}

		/// <summary>
		/// Gets the current microphone mute state.
		/// </summary>
		[PublicAPI]
		public bool PrivacyMuted
		{
			get { return m_StateSection.Execute(() => m_PrivacyMuted); }
			protected set
			{
				m_StateSection.Enter();

				try
				{
					if (value == m_PrivacyMuted)
						return;

					m_PrivacyMuted = value;

					Log(eSeverity.Informational, "PrivacyMuted set to {0}", m_PrivacyMuted);
				}
				finally
				{
					m_StateSection.Leave();
				}

				OnPrivacyMuteChanged.Raise(this, new BoolEventArgs(m_PrivacyMuted));
			}
		}

		/// <summary>
		/// Gets the DoNotDisturb state.
		/// </summary>
		[PublicAPI]
		public bool DoNotDisturb
		{
			get { return m_StateSection.Execute(() => m_DoNotDisturb); }
			protected set
			{
				m_StateSection.Enter();

				try
				{
					if (value == m_DoNotDisturb)
						return;

					m_DoNotDisturb = value;

					Log(eSeverity.Informational, "DoNotDisturb set to {0}", m_DoNotDisturb);
				}
				finally
				{
					m_StateSection.Leave();
				}

				OnDoNotDisturbChanged.Raise(this, new BoolEventArgs(m_DoNotDisturb));
			}
		}

		/// <summary>
		/// Gets the type of conference this dialer supports.
		/// </summary>
		public abstract eConferenceSourceType Supports { get; }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		protected AbstractDialingDeviceControl(T parent, int id)
			: base(parent, id)
		{
			m_StateSection = new SafeCriticalSection();
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnDoNotDisturbChanged = null;
			OnAutoAnswerChanged = null;
			OnPrivacyMuteChanged = null;

			base.DisposeFinal(disposing);
		}

		#region Methods

		/// <summary>
		/// Gets the active conference sources.
		/// </summary>
		/// <returns></returns>
		public abstract IEnumerable<IConferenceSource> GetSources();

		/// <summary>
		/// Dials the given number.
		/// </summary>
		/// <param name="number"></param>
		public abstract void Dial(string number);

		/// <summary>
		/// Dials the given number.
		/// </summary>
		/// <param name="number"></param>
		/// <param name="callType"></param>
		public abstract void Dial(string number, eConferenceSourceType callType);

		public virtual void Dial(IContact contact)
		{
			if (contact == null)
				throw new ArgumentNullException("contact");

			if (!contact.GetContactMethods().Any())
				throw new InvalidOperationException(string.Format("No contact methods for contact {0}", contact.Name));

			IContactMethod contactMethod;
			if (!contact.GetContactMethods().TryFirst(cm => !string.IsNullOrEmpty(cm.Number), out contactMethod))
				throw new InvalidOperationException(string.Format("No contact methods for contact {0} have a valid number", contact.Name));

			Dial(contactMethod.Number);
		}

		/// <summary>
		/// Returns the level of support the device has for the given booking.
		/// </summary>
		/// <param name="bookingNumber"></param>
		/// <returns></returns>
		public abstract eBookingSupport CanDial(IBookingNumber bookingNumber);

		/// <summary>
		/// Dials the given booking.
		/// </summary>
		/// <param name="bookingNumber"></param>
		public abstract void Dial(IBookingNumber bookingNumber);

		/// <summary>
		/// Sets the do-not-disturb enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public abstract void SetDoNotDisturb(bool enabled);

		/// <summary>
		/// Sets the auto-answer enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public abstract void SetAutoAnswer(bool enabled);

		/// <summary>
		/// Sets the privacy mute enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public abstract void SetPrivacyMute(bool enabled);

		#endregion

		#region Source Events

		/// <summary>
		/// Subscribes to the source events in order to re-raise OnSourceChanged event.
		/// </summary>
		/// <param name="source"></param>
		protected void SourceSubscribe(IConferenceSource source)
		{
			source.OnAnswerStateChanged += SourceOnPropertyChanged;
			source.OnNameChanged += SourceOnPropertyChanged;
			source.OnNumberChanged += SourceOnPropertyChanged;
			source.OnSourceTypeChanged += SourceOnPropertyChanged;
			source.OnStatusChanged += SourceOnPropertyChanged;
		}

		/// <summary>
		/// Unsubscribes from the source events to stop re-raising OnSourceChanged event.
		/// </summary>
		/// <param name="source"></param>
		protected void SourceUnsubscribe(IConferenceSource source)
		{
			source.OnAnswerStateChanged -= SourceOnPropertyChanged;
			source.OnNameChanged -= SourceOnPropertyChanged;
			source.OnNumberChanged -= SourceOnPropertyChanged;
			source.OnSourceTypeChanged -= SourceOnPropertyChanged;
			source.OnStatusChanged -= SourceOnPropertyChanged;
		}

		private void SourceOnPropertyChanged(object sender, EventArgs args)
		{
			OnSourceChanged.Raise(this, new ConferenceSourceEventArgs(sender as IConferenceSource));
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

			foreach (IConsoleCommand command in DialingDeviceControlConsole.GetConsoleCommands(this))
				yield return command;
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
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

			DialingDeviceControlConsole.BuildConsoleStatus(this, addRow);
		}

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			foreach (IConsoleNodeBase node in DialingDeviceControlConsole.GetConsoleNodes(this))
				yield return node;
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
	}
}