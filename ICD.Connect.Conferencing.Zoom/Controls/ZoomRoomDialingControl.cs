using System;
using System.Collections.Generic;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.Contacts;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Zoom.Components.Call;
using ICD.Connect.Conferencing.Zoom.Components.Directory;
using ICD.Connect.Conferencing.Zoom.Models;
using ICD.Connect.Conferencing.Zoom.Responses;

namespace ICD.Connect.Conferencing.Zoom.Controls
{
	public sealed class ZoomRoomDialingControl : AbstractDialingDeviceControl<ZoomRoom>
	{

		public override event EventHandler<ConferenceSourceEventArgs> OnSourceAdded;
		public override event EventHandler<ConferenceSourceEventArgs> OnSourceRemoved;

		private readonly CallComponent m_Component;

		#region Properties

		public override eConferenceSourceType Supports
		{
			get { return eConferenceSourceType.Video; }
		}

		private eCallStatus m_CallStatus;
		public eCallStatus CallStatus
		{
			get { return m_CallStatus; }
			private set
			{
				if (m_CallStatus == value)
					return;
				m_CallStatus = value;

				if (m_CallStatus == eCallStatus.IN_MEETING)
					OnSourceAdded.Raise(this, new ConferenceSourceEventArgs(m_Component));
				else if (m_CallStatus == eCallStatus.NOT_IN_MEETING)
					OnSourceRemoved.Raise(this, new ConferenceSourceEventArgs(m_Component));
			}
		}

		#endregion

		public ZoomRoomDialingControl(ZoomRoom parent, int id) : base(parent, id)
		{
			m_Component = Parent.Components.GetComponent<CallComponent>();
			Subscribe(parent);
		}

		#region Methods

		public override IEnumerable<IConferenceSource> GetSources()
		{
			var call = Parent.CurrentCall;
			if (call != null)
				yield return call;
		}

		public override void Dial(string number)
		{
			Parent.Log(eSeverity.Warning, "Zoom Room does not support dialing SIP numbers. Dial a contact instead");
		}

		public override void Dial(string number, eConferenceSourceType callType)
		{
			Dial(number);
		}

		public override void Dial(IContact contact)
		{
			if (contact == null)
				throw new ArgumentNullException();

			var zoomContact = contact as ZoomContact;
			if (zoomContact != null)
			{
				if (CallStatus == eCallStatus.IN_MEETING)
					InviteUser(zoomContact);
				else
				{
					ZoomRoom.ResponseCallback<InfoResultResponse> inviteContactOnCallStart = null;
					inviteContactOnCallStart = (a, b) =>
					{
						Parent.UnregisterResponseCallback(inviteContactOnCallStart);
						InviteUser(zoomContact);
					};
					Parent.RegisterResponseCallback(inviteContactOnCallStart);
					Parent.Log(eSeverity.Debug, "Starting personal Zoom meeting to invite user");
					Parent.SendCommand("zCommand Dial StartPmi Duration: 30");
				}
				
				return;
			}
				
			var zoomBooking = contact as ZoomBooking;
			if (zoomBooking != null)
			{
				Parent.SendCommand("zCommand Dial Start meetingNumber: {0}", zoomBooking.MeetingNumber);
				return;
			}
			
			Parent.Log(eSeverity.Error, "Zoom Room can not handle contacts of type {0}", contact.GetType().Name);
		}

		public override void SetDoNotDisturb(bool enabled)
		{
			Parent.DoNotDisturb = enabled;
		}

		public override void SetAutoAnswer(bool enabled)
		{
			Parent.AutoAnswer = enabled;
		}

		public override void SetPrivacyMute(bool enabled)
		{
			Parent.SendCommand("zConfiguration Call Microphone mute: on");
		}

		#endregion

		private void InviteUser(ZoomContact zoomContact)
		{
			Parent.Log(eSeverity.Debug, "Inviting {0} to Zoom meeting", zoomContact.Name);
			Parent.SendCommand("zCommand Call Invite user: {0}", zoomContact.JoinId);
		}

		#region Parent Callbacks

		private void Subscribe(ZoomRoom parent)
		{
			parent.RegisterResponseCallback<CallStatusResponse>(CallStatusCallback);
		}

		private void CallStatusCallback(ZoomRoom zoomroom, CallStatusResponse response)
		{
			CallStatus = response.CallStatus.Status;
		}

		#endregion
	}
}