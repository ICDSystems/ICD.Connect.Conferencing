using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using ICD.Connect.Conferencing.Controls.Routing;
using ICD.Connect.Routing;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.EventArguments;

namespace ICD.Connect.Conferencing.Zoom.Controls
{
	public class ZoomRoomRoutingControl : AbstractVideoConferenceRouteControl<ZoomRoom>
	{
		public override event EventHandler<TransmissionStateEventArgs> OnActiveTransmissionStateChanged;
		public override event EventHandler<SourceDetectionStateChangeEventArgs> OnSourceDetectionStateChange;
		public override event EventHandler<ActiveInputStateChangeEventArgs> OnActiveInputsChanged;

		public ZoomRoomRoutingControl(ZoomRoom parent, int id) : base(parent, id)
		{
		}

		public override void SetCameraInput(int address)
		{
			throw new NotImplementedException();
		}

		public override bool GetActiveTransmissionState(int output, eConnectionType type)
		{
			return true;
		}

		public override IEnumerable<ConnectorInfo> GetOutputs()
		{
			yield return new ConnectorInfo(1, eConnectionType.Audio | eConnectionType.Video);
			yield return new ConnectorInfo(2, eConnectionType.Audio | eConnectionType.Video);
		}

		public override bool GetSignalDetectedState(int input, eConnectionType type)
		{
			return true;
		}

		public override bool GetInputActiveState(int input, eConnectionType type)
		{
			return true;
		}

		public override IEnumerable<ConnectorInfo> GetInputs()
		{
			yield return new ConnectorInfo(1, eConnectionType.Audio | eConnectionType.Video);
		}
	}
}