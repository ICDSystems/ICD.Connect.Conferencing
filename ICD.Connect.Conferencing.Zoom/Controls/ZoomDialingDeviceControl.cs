using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.Contacts;
using ICD.Connect.Conferencing.Controls;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Zoom.Responses;

namespace ICD.Connect.Conferencing.Zoom.Controls
{
	public sealed class ZoomDialingDeviceControl : AbstractDialingDeviceControl<ZoomRoom>
	{
		public ZoomDialingDeviceControl(ZoomRoom parent, int id) : base(parent, id)
		{
		}

		public override event EventHandler<ConferenceSourceEventArgs> OnSourceAdded;
		public override event EventHandler<ConferenceSourceEventArgs> OnSourceRemoved;

		public override eConferenceSourceType Supports
		{
			get { return eConferenceSourceType.Video; }
		}

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
				ZoomRoom.ResponseCallback<InfoResultResponse> inviteContactOnCallStart = null;
				inviteContactOnCallStart = (a, b) =>
				{
					Parent.UnregisterResponseCallback(inviteContactOnCallStart);
					Parent.Log(eSeverity.Debug, "Inviting {0} to Zoom meeting", zoomContact.Name);
					Parent.SendCommand("zCommand Call Invite user: {0}", zoomContact.JoinId);
				};
				Parent.RegisterResponseCallback(inviteContactOnCallStart);
				Parent.Log(eSeverity.Debug, "Starting personal Zoom meeting");
				Parent.SendCommand("zCommand Dial StartPmi Duration: 30");
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
	}
}