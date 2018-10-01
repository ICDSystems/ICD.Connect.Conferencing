using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Calendaring;
using ICD.Connect.Calendaring.Booking;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.Contacts;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Zoom.Components.Call;
using ICD.Connect.Conferencing.Zoom.Components.Directory;
using ICD.Connect.Conferencing.Zoom.Controls.Calendar;
using ICD.Connect.Conferencing.Zoom.Responses;

namespace ICD.Connect.Conferencing.Zoom.Controls
{
	public sealed class ZoomRoomDialingControl : AbstractDialingDeviceControl<ZoomRoom>
	{

		public override event EventHandler<ConferenceSourceEventArgs> OnSourceAdded;
		public override event EventHandler<ConferenceSourceEventArgs> OnSourceRemoved;

		private readonly CallComponent m_CurrentCall;
		private readonly Dictionary<string, ThinConferenceSource> m_ParticipantSources;

		private readonly List<CallComponent> m_IncomingCalls;

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
					OnSourceAdded.Raise(this, new ConferenceSourceEventArgs(m_CurrentCall));
				else if (m_CallStatus == eCallStatus.NOT_IN_MEETING)
					OnSourceRemoved.Raise(this, new ConferenceSourceEventArgs(m_CurrentCall));
			}
		}

		#endregion

		public ZoomRoomDialingControl(ZoomRoom parent, int id) : base(parent, id)
		{
			m_CurrentCall = Parent.Components.GetComponent<CallComponent>();
			Subscribe(m_CurrentCall);
			m_ParticipantSources = new Dictionary<string, ThinConferenceSource>();
			m_IncomingCalls = new List<CallComponent>();
			Subscribe(parent);
		}

		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			Unsubscribe(Parent);
		}

		#region Methods

		public override IEnumerable<IConferenceSource> GetSources()
		{
			yield return m_CurrentCall;

			if (CallStatus == eCallStatus.IN_MEETING)
				foreach (var participant in m_ParticipantSources)
					yield return participant.Value;
		}

		public override void Dial(string number)
		{
			var meetingNumberMatch = Regex.Match(number, @"^\d{3}-?\d{3}-?\d{4}$");
			if (meetingNumberMatch.Success)
			{
				Parent.SendCommand("zCommand Dial Start meetingNumber: {0}", number);
				return;
			}

			var userJoinIdMatch = Regex.Match(number, @"^\S+@xmpp\.zoom\.us$");
			if(userJoinIdMatch.Success)
			{
				StartMeetingAndInviteUser(number);
				return;
			}

			Parent.Log(eSeverity.Warning, "Could not manually dial \"{0}\"", number);
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
					InviteUser(zoomContact.JoinId);
				else
					StartMeetingAndInviteUser(zoomContact.JoinId);
				
				return;
			}

			Parent.Log(eSeverity.Error, "Zoom Room can not handle contacts of type {0}", contact.GetType().Name);
		}

		public override eBookingSupport CanDial(IBookingNumber bookingNumber)
		{
			if (bookingNumber is ZoomBookingNumber)
				return eBookingSupport.Native;

			var zoomNumber = bookingNumber as IZoomBookingNumber;
			if (zoomNumber != null && !string.IsNullOrEmpty(zoomNumber.MeetingNumber))
				return eBookingSupport.ParsedNative;

			return eBookingSupport.Unsupported;
		}

		public override void Dial(IBookingNumber bookingNumber)
		{
			var zoomNumber = bookingNumber as IZoomBookingNumber;
			if (zoomNumber == null || string.IsNullOrEmpty(zoomNumber.MeetingNumber))
			{
				Log(eSeverity.Error, "Cannot dial booking - Not a valid Zoom booking");
				return;
			}

			Dial(zoomNumber.MeetingNumber);
		}

		public override void SetDoNotDisturb(bool enabled)
		{
			Parent.DoNotDisturb = enabled;
		}

		public override void SetAutoAnswer(bool enabled)
		{
			Parent.Log(eSeverity.Warning, "Zoom Room does not support setting auto-answer through the SSH API");
		}

		public override void SetPrivacyMute(bool enabled)
		{
			Parent.SendCommand("zConfiguration Call Microphone mute: {0}", enabled ? "on" : "off");
			//Parent.SendCommand("zConfiguration Call Camera mute: {0}", enabled ? "on" : "off");
		}

		#endregion
		
		#region Private Methods

		private void StartMeetingAndInviteUser(string userJoinId)
		{
			ZoomRoom.ResponseCallback<InfoResultResponse> inviteContactOnCallStart = null;
			inviteContactOnCallStart = (a, b) =>
			{
				Parent.UnregisterResponseCallback(inviteContactOnCallStart);
				InviteUser(userJoinId);
			};
			Parent.RegisterResponseCallback(inviteContactOnCallStart);
			if (CallStatus != eCallStatus.CONNECTING_MEETING || CallStatus == eCallStatus.LOGGED_OUT)
			{
				Parent.Log(eSeverity.Debug, "Starting personal Zoom meeting to invite user");
				Parent.SendCommand("zCommand Dial StartPmi Duration: 30");
			}
		}

		private void InviteUser(string userJoinId)
		{
			Parent.Log(eSeverity.Informational, "Inviting user: {0}", userJoinId);
			Parent.SendCommand("zCommand Call Invite user: {0}", userJoinId);
		}

		#endregion

		#region Parent Callbacks
		
		private void Subscribe(ZoomRoom parent)
		{
			parent.RegisterResponseCallback<CallStatusResponse>(CallStatusCallback);
			parent.RegisterResponseCallback<IncomingCallResponse>(IncomingCallCallback);
			parent.RegisterResponseCallback<CallConfigurationResponse>(CallConfigurationCallback);

			parent.OnInitializedChanged += ParentOnOnInitializedChanged;
		}

		private void Unsubscribe(ZoomRoom parent)
		{
			parent.UnregisterResponseCallback<CallStatusResponse>(CallStatusCallback);
			parent.UnregisterResponseCallback<IncomingCallResponse>(IncomingCallCallback);
			parent.UnregisterResponseCallback<CallConfigurationResponse>(CallConfigurationCallback);

			parent.OnInitializedChanged += ParentOnOnInitializedChanged;
		}

		private void CallStatusCallback(ZoomRoom zoomroom, CallStatusResponse response)
		{
			CallStatus = response.CallStatus.Status;
		}

		private void IncomingCallCallback(ZoomRoom zoomroom, IncomingCallResponse response)
		{
			var incomingCall = new CallComponent(response.IncomingCall, zoomroom);

			m_IncomingCalls.Add(incomingCall);
			SourceSubscribe(incomingCall);
			OnSourceAdded.Raise(this, new ConferenceSourceEventArgs(incomingCall));
			Parent.Log(eSeverity.Informational, "Incoming call: {0}", response.IncomingCall.CallerName);
		}

		private void CallConfigurationCallback(ZoomRoom zoomRoom, CallConfigurationResponse response)
		{
			var configuration = response.CallConfiguration;
			if (configuration.Microphone != null)
				PrivacyMuted = configuration.Microphone.Mute;
		}

		private void ParentOnOnInitializedChanged(object sender, BoolEventArgs e)
		{
			Parent.SendCommand("zStatus Call Status");
			Parent.SendCommand("zConfiguration Call Microphone");
		}

		#endregion

		#region CallComponent Callbacks

		private void Subscribe(CallComponent currentCall)
		{
			currentCall.OnParticipantAdded += CurrentCallOnOnParticipantAdded;
			currentCall.OnParticipantRemoved += CurrentCallOnOnParticipantRemoved;
			currentCall.OnParticipantUpdated += CurrentCallOnOnParticipantUpdated;
		}

		private void CurrentCallOnOnParticipantAdded(object sender, GenericEventArgs<ParticipantInfo> args)
		{
			var source = new ThinConferenceSource();
			var participant = args.Data;
			source.Name = participant.UserName;
			source.Number = participant.UserId;
			source.Start = IcdEnvironment.GetLocalTime();
			source.Direction = eConferenceSourceDirection.Undefined;
			source.Status = eConferenceSourceStatus.Connected;
			source.HoldCallback = (s) => { MuteParticipant(true, s.Number); };
			source.ResumeCallback = (s) => { MuteParticipant(false, s.Number); };
			source.SendDtmfCallback = (s,s2) => { Parent.Log(eSeverity.Warning, "Zoom Room does not support sending DTMF"); };
			source.AnswerState = eConferenceSourceAnswerState.Autoanswered;

			SourceSubscribe(source);
			OnSourceAdded.Raise(this, new ConferenceSourceEventArgs(source));
			m_ParticipantSources.Add(args.Data.UserId, source);
		}

		private void CurrentCallOnOnParticipantRemoved(object sender, GenericEventArgs<ParticipantInfo> args)
		{
			var source = m_ParticipantSources[args.Data.UserId];
			source.End = IcdEnvironment.GetLocalTime();
			source.Status = eConferenceSourceStatus.Disconnected;

			OnSourceRemoved.Raise(this, new ConferenceSourceEventArgs(source));
			m_ParticipantSources.Remove(args.Data.UserId);
		}

		private void CurrentCallOnOnParticipantUpdated(object sender, GenericEventArgs<ParticipantInfo> args)
		{
			var participant = args.Data;
			var source = m_ParticipantSources[participant.UserId];
			source.Name = participant.UserName;
		}

		private void MuteParticipant(bool mute, string userId)
		{
			Parent.SendCommand("zCommand Call MuteParticipant mute: {0} Id: {1}", mute ? "on" : "off", userId);
			Parent.SendCommand("zCommand Call MuteParticipantVideo Mute: {0} Id: {1}", mute ? "on" : "off", userId);
		}
		#endregion
	}
}