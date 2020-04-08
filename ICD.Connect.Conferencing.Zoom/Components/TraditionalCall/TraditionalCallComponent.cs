using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Conferencing.Zoom.Responses;

namespace ICD.Connect.Conferencing.Zoom.Components.TraditionalCall
{
	public sealed class TraditionalCallComponent : AbstractZoomRoomComponent
	{
		public event EventHandler<GenericEventArgs<TraditionalZoomPhoneCallInfo>> OnCallStatusChanged;
		public event EventHandler<GenericEventArgs<TraditionalZoomPhoneCallInfo>> OnCallTerminated;
		public event EventHandler<GenericEventArgs<PhoneCallTerminated>> OnCallFailed; 

		private readonly IcdOrderedDictionary<string, TraditionalZoomPhoneCallInfo> m_CallInfos;
		private readonly SafeCriticalSection m_CallInfosSection;

		#region Constructor

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		public TraditionalCallComponent(ZoomRoom parent)
			: base(parent)
		{
			m_CallInfos = new IcdOrderedDictionary<string, TraditionalZoomPhoneCallInfo>();
			m_CallInfosSection = new SafeCriticalSection();

			Subscribe(Parent);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal()
		{
			OnCallStatusChanged = null;
			OnCallTerminated = null;
			OnCallFailed = null;

			base.DisposeFinal();

			Unsubscribe(Parent);
		}

		#endregion

		#region Methods

		public IEnumerable<TraditionalZoomPhoneCallInfo> GetCallInfo()
		{
			return m_CallInfosSection.Execute(() => m_CallInfos.Values.ToArray(m_CallInfos.Count));
		}

		public void PhoneCallOut(string dialString)
		{
			Parent.Logger.Log(eSeverity.Informational, "PSTN Dialing out to: {0}", dialString);
			Parent.SendCommand("zCommand Dial PhoneCallOut Number: {0}", dialString);
		}

		public void Hangup(string callId)
		{
			Parent.Logger.Log(eSeverity.Informational, "Hanging up call with ID: {0}", callId);
			Parent.SendCommand("zCommand Dial PhoneHangUp CallId: {0}", callId);
		}

		public void SendDtmf(string callId, char data)
		{
			Parent.Logger.Log(eSeverity.Informational, "Sending Call with ID: {0} DTMF character: {1}", callId, data);
			Parent.SendCommand("zCommand SendSIPDTMF CallID: {0} Key: {1}", callId, data);
		}

		#endregion

		#region Private Methods

		private void UpdateOrAddInfo(PhoneCallStatus data)
		{
			TraditionalZoomPhoneCallInfo info;

			m_CallInfosSection.Enter();

			try
			{
				info = m_CallInfos.GetOrAddNew(data.CallId);
				info.UpdateStatusInfo(data);
			}
			finally
			{
				m_CallInfosSection.Leave();
			}

			OnCallStatusChanged.Raise(this, new GenericEventArgs<TraditionalZoomPhoneCallInfo>(info));
		}

		private void RemoveInfo(PhoneCallTerminated data)
		{
			TraditionalZoomPhoneCallInfo info;

			m_CallInfosSection.Enter();

			try
			{
				if (!m_CallInfos.TryGetValue(data.CallId, out info))
					return;

				info.UpdateTerminateInfo(data);
				m_CallInfos.Remove(data.CallId);
			}
			finally
			{
				m_CallInfosSection.Leave();
			}

			OnCallTerminated.Raise(this, new GenericEventArgs<TraditionalZoomPhoneCallInfo>(info));
		}

		#endregion

		#region Parent Callbacks

		private void Subscribe(ZoomRoom parent)
		{
			parent.RegisterResponseCallback<PhoneCallStatusResponse>(PhoneCallStatusResponseCallback);
			parent.RegisterResponseCallback<PhoneCallTerminatedResponse>(PhoneCallTerminatedResponse);
		}

		private void Unsubscribe(ZoomRoom parent)
		{
			parent.UnregisterResponseCallback<PhoneCallStatusResponse>(PhoneCallStatusResponseCallback);
			parent.UnregisterResponseCallback<PhoneCallTerminatedResponse>(PhoneCallTerminatedResponse);
		}

		private void PhoneCallStatusResponseCallback(ZoomRoom zoomRoom, PhoneCallStatusResponse response)
		{
			var data = response.PhoneCallStatus;
			if (data == null)
				return;

			// There is currently no way for a ZoomRoom to answer an incoming call, so we reject the call.
			if (data.IsIncomingCall)
			{
				Parent.Logger.Log(eSeverity.Warning, "Unable to answer incoming call via Zoom API. Rejecting.");
				Hangup(data.CallId);
				return;
			}

			UpdateOrAddInfo(data);
		}

		private void PhoneCallTerminatedResponse(ZoomRoom zoomRoom, PhoneCallTerminatedResponse response)
		{
			var data = response.PhoneCallTerminated;
			if (data == null)
				return;

			RemoveInfo(data);

			switch (data.Reason)
			{
				case eZoomPhoneCallTerminatedReason.ByLocal:
				case eZoomPhoneCallTerminatedReason.ByRemote:
					break;

				default:
					OnCallFailed.Raise(this, new GenericEventArgs<PhoneCallTerminated>(data));
					break;
			}
		}

		#endregion

		#region Console

		public override string ConsoleHelp { get { return "Zoom Room Traditional Call"; } }

		#endregion
	}
}
