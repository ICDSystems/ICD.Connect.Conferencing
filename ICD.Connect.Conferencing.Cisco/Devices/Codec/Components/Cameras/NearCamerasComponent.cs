using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Xml;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Common.Logging.LoggingContexts;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Cameras
{
	/// <summary>
	/// NearCamerasComponent provides methods for managing the near cameras.
	/// </summary>
	public sealed class NearCamerasComponent : AbstractCiscoComponent
	{
		/// <summary>
		/// Raised when the cameras are rebuilt.
		/// </summary>
		public event EventHandler OnCamerasChanged;

		/// <summary>
		/// Raises the camera id when presets change.
		/// </summary>
		public event EventHandler<IntEventArgs> OnPresetsChanged;

		/// <summary>
		/// Raised when the presenter track availability changes.
		/// </summary>
		public event EventHandler<PresenterTrackAvailabilityEventArgs> OnPresenterTrackAvailabilityChanged;

		/// <summary>
		/// Raised when a presenter is detected or no longer detected.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnPresenterDetectedStateChanged; 

		/// <summary>
		/// Raised when the presenter track mode changes.
		/// </summary>
		public event EventHandler<PresenterTrackModeEventArgs> OnPresenterTrackModeChanged;

		/// <summary>
		/// Raised when the speaker track availability changes.
		/// </summary>
		public event EventHandler<SpeakerTrackAvailabilityEventArgs> OnSpeakerTrackAvailabilityChanged;

		/// <summary>
		/// Raised when the speaker track status changes.
		/// </summary>
		public event EventHandler<SpeakerTrackStatusEventArgs> OnSpeakerTrackStatusChanged;

		/// <summary>
		/// Raised when the speaker track whiteboard mode changes.
		/// </summary>
		public event EventHandler<SpeakerTrackWhiteboardModeEventArgs> OnSpeakerTrackWhiteboardModeChanged;

		/// <summary>
		/// Raised when the speaker track whiteboard distance changes.
		/// </summary>
		public event EventHandler<IntEventArgs> OnSpeakerTrackWhiteboardDistanceChanged; 

		private readonly IcdOrderedDictionary<int, NearCamera> m_Cameras;
		private readonly IcdOrderedDictionary<int, CiscoCameraPreset> m_Presets;

		private readonly SafeCriticalSection m_CamerasSection;
		private readonly SafeCriticalSection m_PresetsSection;

		private ePresenterTrackMode m_PresenterTrackMode;
		private ePresenterTrackAvailability m_PresenterTrackAvailability;
		private bool m_PresenterDetected;

		private eSpeakerTrackAvailability m_SpeakerTrackAvailability;
		private eSpeakerTrackStatus m_SpeakerTrackStatus;
		private eSpeakerTrackWhiteboardMode m_SpeakerTrackWhiteboardMode;
		private int m_SpeakerTrackWhiteboardDistance;

		#region Properties

		/// <summary>
		/// Gets the number of cameras.
		/// </summary>
		[PublicAPI]
		public int CamerasCount { get { return m_Cameras.Count; } }

		/// <summary>
		/// Gets the presenter track availability.
		/// </summary>
		[PublicAPI]
		public ePresenterTrackAvailability PresenterTrackAvailability
		{
			get { return m_PresenterTrackAvailability; }
			private set
			{
				if (value == m_PresenterTrackAvailability)
					return;

				m_PresenterTrackAvailability = value;

				Codec.Logger.LogSetTo(eSeverity.Informational, "PresenterTrackAvailability", m_PresenterTrackAvailability);

				OnPresenterTrackAvailabilityChanged.Raise(this,
				                                          new PresenterTrackAvailabilityEventArgs(m_PresenterTrackAvailability));
			}
		}

		/// <summary>
		/// Gets the presenter detected state.
		/// </summary>
		[PublicAPI]
		public bool PresenterDetected
		{
			get { return m_PresenterDetected; }
			private set
			{
				if (value == m_PresenterDetected)
					return;

				m_PresenterDetected = value;

				Codec.Logger.LogSetTo(eSeverity.Informational, "PresenterDetected", m_PresenterDetected);

				OnPresenterDetectedStateChanged.Raise(this, new BoolEventArgs(m_PresenterDetected));
			}
		}

		/// <summary>
		/// Gets the presenter track mode.
		/// </summary>
		[PublicAPI]
		public ePresenterTrackMode PresenterTrackMode
		{
			get { return m_PresenterTrackMode; }
			private set
			{
				if (value == m_PresenterTrackMode)
					return;
				
				m_PresenterTrackMode = value;

				Codec.Logger.LogSetTo(eSeverity.Informational, "PresenterTrackMode", m_PresenterTrackMode);

				OnPresenterTrackModeChanged.Raise(this, new PresenterTrackModeEventArgs(m_PresenterTrackMode));
			}
		}

		/// <summary>
		/// Gets the camera id that is configured for presenter track.
		/// </summary>
		public int? PresenterTrackCameraId { get { return Codec.PresenterTrackCameraId; } }

		/// <summary>
		/// Gets the speaker track availability.
		/// </summary>
		[PublicAPI]
		public eSpeakerTrackAvailability SpeakerTrackAvailability
		{
			get { return m_SpeakerTrackAvailability; }
			private set
			{
				if (value == m_SpeakerTrackAvailability)
					return;
				
				m_SpeakerTrackAvailability = value;

				Codec.Logger.LogSetTo(eSeverity.Informational, "SpeakerTrackAvailability", m_SpeakerTrackAvailability);

				OnSpeakerTrackAvailabilityChanged.Raise(this, new SpeakerTrackAvailabilityEventArgs(m_SpeakerTrackAvailability));
			}
		}

		/// <summary>
		/// Gets the speaker track status.
		/// </summary>
		[PublicAPI]
		public eSpeakerTrackStatus SpeakerTrackStatus
		{
			get { return m_SpeakerTrackStatus; }
			private set
			{
				if (value == m_SpeakerTrackStatus)
					return;

				m_SpeakerTrackStatus = value;

				Codec.Logger.LogSetTo(eSeverity.Informational, "SpeakerTrackStatus", m_SpeakerTrackStatus);

				OnSpeakerTrackStatusChanged.Raise(this, new SpeakerTrackStatusEventArgs(m_SpeakerTrackStatus));
			}
		}

		/// <summary>
		/// Gets the speaker track whiteboard mode.
		/// </summary>
		[PublicAPI]
		public eSpeakerTrackWhiteboardMode SpeakerTrackWhiteboardMode
		{
			get { return m_SpeakerTrackWhiteboardMode; }
			private set
			{
				if (value == m_SpeakerTrackWhiteboardMode)
					return;

				m_SpeakerTrackWhiteboardMode = value;

				Codec.Logger.LogSetTo(eSeverity.Informational, "SpeakerTrackWhiteboardMode", m_SpeakerTrackWhiteboardMode);

				OnSpeakerTrackWhiteboardModeChanged.Raise(this, new SpeakerTrackWhiteboardModeEventArgs(m_SpeakerTrackWhiteboardMode));
			}
		}

		/// <summary>
		/// Gets the speaker track whiteboard distance in centimeters.
		/// </summary>
		[PublicAPI]
		public int SpeakerTrackWhiteboardDistance
		{
			get { return m_SpeakerTrackWhiteboardDistance; }
			private set
			{
				if (value == m_SpeakerTrackWhiteboardDistance)
					return;

				m_SpeakerTrackWhiteboardDistance = value;

				Codec.Logger.LogSetTo(eSeverity.Informational, "SpeakerTrack Whiteboard Distance", m_SpeakerTrackWhiteboardDistance);

				OnSpeakerTrackWhiteboardDistanceChanged.Raise(this, new IntEventArgs(m_SpeakerTrackWhiteboardDistance));
			}
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="codec"></param>
		public NearCamerasComponent(CiscoCodecDevice codec)
			: base(codec)
		{
			m_Cameras = new IcdOrderedDictionary<int, NearCamera>();
			m_Presets = new IcdOrderedDictionary<int, CiscoCameraPreset>();

			m_CamerasSection = new SafeCriticalSection();
			m_PresetsSection = new SafeCriticalSection();

			Subscribe(Codec);

			if (Codec.Initialized)
				Initialize();
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			OnCamerasChanged = null;
			OnPresetsChanged = null;

			OnPresenterTrackAvailabilityChanged = null;
			OnPresenterDetectedStateChanged = null;
			OnPresenterTrackModeChanged = null;

			OnSpeakerTrackAvailabilityChanged = null;
			OnSpeakerTrackStatusChanged = null;
			OnSpeakerTrackWhiteboardModeChanged = null;
			OnSpeakerTrackWhiteboardDistanceChanged = null;

			base.Dispose(disposing);
		}

		#endregion

		#region Methods

		/// <summary>
		/// Gets the camera with the given id. Lazy Loads if the camera is not loaded.
		/// </summary>
		/// <param name="cameraId"></param>
		/// <returns></returns>
		[PublicAPI]
		public NearCamera GetCamera(int cameraId)
		{
			m_CamerasSection.Enter();

			try
			{
				return m_Cameras.GetOrAddNew(cameraId, () => new NearCamera(cameraId, Codec));
			}
			finally
			{
				m_CamerasSection.Leave();
			}
		}

		/// <summary>
		/// Gets all of the available cameras.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		public IEnumerable<NearCamera> GetCameras()
		{
			return m_CamerasSection.Execute(() => m_Cameras.Values.ToArray());
		}

		/// <summary>
		/// Returns the cameras that are connected to the system.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		public IEnumerable<NearCamera> GetConnectedCameras()
		{
			return GetCameras().Where(c => c.Connected);
		}

		public IEnumerable<CiscoCameraPreset> GetPresets()
		{
			return m_PresetsSection.Execute(() => m_Presets.Values.ToArray());
		} 

		/// <summary>
		/// Updates the list of presets.
		/// </summary>
		public void ListPresets()
		{
			Codec.SendCommand("xCommand Camera Preset List");
		}

		/// <summary>
		/// Activates the preset with the given id.
		/// </summary>
		/// <param name="cameraId"></param>
		/// <param name="presetId"></param>
		public void ActivatePreset(int cameraId, int presetId)
		{
			Codec.Logger.Log(eSeverity.Informational, "Activating Preset {0} for Camera {1}", presetId, cameraId);
			Codec.SendCommand("xCommand Camera Preset Activate PresetId: {0}", presetId);
		}

		/// <summary>
		/// Removes the preset with the given id.
		/// </summary>
		/// <param name="presetId"></param>
		[PublicAPI]
		public void RemovePreset(int presetId)
		{
			Codec.Logger.Log(eSeverity.Informational, "Removing Preset {0}", presetId);
			Codec.SendCommand("xCommand Camera Preset Remove PresetId: {0}", presetId);

			ListPresets();
		}

		/// <summary>
		/// Removes all of the camera presets.
		/// </summary>
		[PublicAPI]
		public void RemovePresets()
		{
			IEnumerable<int> presetIds = m_CamerasSection.Execute(() => m_Presets.Keys.ToArray());

			foreach (int presetId in presetIds)
				RemovePreset(presetId);
		}

		/// <summary>
		/// Stores the given camera position as the preset with the given id.
		/// </summary>
		/// <param name="cameraId"></param>
		/// <param name="name"></param>
		/// <param name="presetId"></param>
		public void StorePreset(int cameraId, string name, int presetId)
		{
			Codec.Logger.Log(eSeverity.Informational, "Storing preset {0} for Camera {1}", presetId, cameraId);
			Codec.SendCommand("xCommand Camera Preset Store CameraId: {0} PresetId: {1} Name: {2}", cameraId, presetId, name);

			ListPresets();
		}

		/// <summary>
		/// Sets the presenter track mode.
		/// </summary>
		/// <param name="mode"></param>
		[PublicAPI]
		public void SetPresenterTrackMode(ePresenterTrackMode mode)
		{
			Codec.SendCommand("xCommand Cameras PresenterTrack Set Mode: {0}", mode);
			Codec.Logger.Log(eSeverity.Informational, "Setting PresenterTrack mode to {0}", mode);
		}

		/// <summary>
		/// Activates the speaker track.
		/// </summary>
		[PublicAPI]
		public void ActivateSpeakerTrack()
		{
			Codec.SendCommand("xCommand Cameras SpeakerTrack Activate");
			Codec.Logger.Log(eSeverity.Informational, "Setting SpeakerTrack active");
		}

		/// <summary>
		/// Deactivates the speaker track.
		/// </summary>
		[PublicAPI]
		public void DeactivateSpeakerTrack()
		{
			Codec.SendCommand("xCommand Cameras SpeakerTrack Deactivate");
			Codec.Logger.Log(eSeverity.Informational, "Setting SpeakerTrack inactive");
		}

		/// <summary>
		/// Sets the SpeakerTrack Whiteboard Mode.
		/// </summary>
		/// <param name="mode"></param>
		[PublicAPI]
		public void SetSpeakerTrackWhiteboardMode(eSpeakerTrackWhiteboardMode mode)
		{
			Codec.SendCommand("xCommand Cameras SpeakerTrack Whiteboard Mode: {0}", mode);
			Codec.Logger.Log(eSeverity.Informational, "Setting SpeakerTrack Whiteboard Mode to {0}", mode);
		}

		/// <summary>
		/// Sets the SpeakerTrack Whiteboard Distance in centimeters.
		/// </summary>
		/// <param name="centimeters"></param>
		[PublicAPI]
		public void SetSpeakerTrackWhiteboardDistance(ushort centimeters)
		{
			// Documentation says only whiteboard 1 is supported
			SetSpeakerTrackWhiteboardDistance(centimeters, 1);
		}

		/// <summary>
		/// Sets the SpeakerTrack Whiteboard Distance in centimeters for the given whiteboard.
		/// </summary>
		/// <param name="centimeters"></param>
		/// <param name="whiteboardId"></param>
		[PublicAPI]
		public void SetSpeakerTrackWhiteboardDistance(ushort centimeters, int whiteboardId)
		{
			Codec.SendCommand("xCommand Cameras SpeakerTrack Whiteboard SetDistance Distance: {0} WhiteboardId: {1}",
							  centimeters, whiteboardId);
			Codec.Logger.Log(eSeverity.Informational,
					  "Setting SpeakerTrack Whiteboard Distance of {0}cm for WhiteboardId {1}",
					  centimeters, whiteboardId);
		}

		/// <summary>
		/// Aligns the SpeakerTrack Whiteboard Position for the given camera and distance in centimeters.
		/// </summary>
		/// <param name="cameraId"></param>
		/// <param name="centimeters"></param>
		[PublicAPI]
		public void AlignSpeakerTrackWhiteboardPosition(int cameraId, ushort centimeters)
		{
			Codec.SendCommand("xCommand Cameras SpeakerTrack Whiteboard AlignPosition CameraId: {0} Distance: {1}",
			                  cameraId, centimeters);
			Codec.Logger.Log(eSeverity.Informational,
			          "Aligning SpeakerTrack Whiteboard Position for CameraId {0} with Distance {1}cm",
			          cameraId, centimeters);
		}

		/// <summary>
		/// Activates the SpeakerTrack Whiteboard Position for the given camera.
		/// </summary>
		/// <param name="cameraId"></param>
		[PublicAPI]
		public void ActivateSpeakerTrackWhiteboardPosition(int cameraId)
		{
			// Documentation says only whiteboard 1 is supported
			ActivateSpeakerTrackWhiteboardPosition(cameraId, 1);
		}

		/// <summary>
		/// Activates the SpeakerTrack Whiteboard Position for the given camera and whiteboard.
		/// </summary>
		/// <param name="cameraId"></param>
		/// <param name="whiteboardId"></param>
		[PublicAPI]
		public void ActivateSpeakerTrackWhiteboardPosition(int cameraId, int whiteboardId)
		{
			Codec.SendCommand("xCommand Cameras SpeakerTrack Whiteboard ActivatePosition CameraId: {0} WhiteboardId: {1}",
			                  cameraId, whiteboardId);
			Codec.Logger.Log(eSeverity.Informational,
			          "Activating SpeakerTrack Whiteboard Position for CameraId {0} and WhiteboardId {1}",
			          cameraId, whiteboardId);
		}

		/// <summary>
		/// Stores the SpeakerTrack Whiteboard Position for the given camera.
		/// </summary>
		/// <param name="cameraId"></param>
		[PublicAPI]
		public void StoreSpeakerTrackWhiteboardPosition(int cameraId)
		{
			// Documentation says only whiteboard 1 is supported
			StoreSpeakerTrackWhiteboardPosition(cameraId, 1);
		}

		/// <summary>
		/// Stores the SpeakerTrack Whiteboard Position for the given camera and whiteboard.
		/// </summary>
		/// <param name="cameraId"></param>
		/// <param name="whiteboardId"></param>
		[PublicAPI]
		public void StoreSpeakerTrackWhiteboardPosition(int cameraId, int whiteboardId)
		{
			Codec.SendCommand("xCommand Cameras SpeakerTrack Whiteboard StorePosition CameraId: {0} WhiteboardId: {1}",
							  cameraId, whiteboardId);
			Codec.Logger.Log(eSeverity.Informational,
					  "Storing SpeakerTrack Whiteboard Position for CameraId {0} and WhiteboardId {1}",
					  cameraId, whiteboardId);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Instantiates a camera preset from a Preset element.
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="cameraId"></param>
		/// <returns></returns>
		private static CiscoCameraPreset CameraPresetFromXml(string xml, out int cameraId)
		{
			cameraId = 0;

			using (IcdXmlReader reader = new IcdXmlReader(xml))
			{
				reader.ReadToNextElement();

				int presetId = 0;
				string name = null;

				foreach (IcdXmlReader child in reader.GetChildElements())
				{
					switch (child.Name)
					{
						case "CameraId":
							cameraId = child.ReadElementContentAsInt();
							break;

						case "PresetId":
							presetId = child.ReadElementContentAsInt();
							break;

						case "Name":
							name = child.ReadElementContentAsString();
							break;
					}

					child.Dispose();
				}

				return new CiscoCameraPreset(cameraId, name, presetId);
			}
		}

		#endregion

		#region Codec Feedback

		/// <summary>
		/// Called to initialize the component.
		/// </summary>
		protected override void Initialize()
		{
			base.Initialize();

			// Initial query to populate the camera presets
			ListPresets();
		}

		/// <summary>
		/// Subscribes to the codec events.
		/// </summary>
		/// <param name="codec"></param>
		protected override void Subscribe(CiscoCodecDevice codec)
		{
			base.Subscribe(codec);

			if (codec == null)
				return;

			codec.RegisterParserCallback(ParseCameraStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "Cameras");
			codec.RegisterParserCallback(ParseCameraPresets, "PresetListResult");

			codec.RegisterParserCallback(ParsePresenterTrackAvailability, CiscoCodecDevice.XSTATUS_ELEMENT, "Cameras", "PresenterTrack", "Availability");
			codec.RegisterParserCallback(ParsePresenterTrackPresenterDetected, CiscoCodecDevice.XSTATUS_ELEMENT, "Cameras", "PresenterTrack", "PresenterDetected");
			codec.RegisterParserCallback(ParsePresenterTrackStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "Cameras", "PresenterTrack", "Status");

			codec.RegisterParserCallback(ParseSpeakerTrackAvailability, CiscoCodecDevice.XSTATUS_ELEMENT, "Cameras", "SpeakerTrack", "Availability");
			codec.RegisterParserCallback(ParseSpeakerTrackStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "Cameras", "SpeakerTrack", "Status");

			codec.RegisterParserCallback(ParseSpeakerTrackWhiteboardMode, CiscoCodecDevice.XCONFIGURATION_ELEMENT, "Cameras", "SpeakerTrack", "Whiteboard", "Mode");
			codec.RegisterParserCallback(ParseSpeakerTrackWhiteboardDistance, CiscoCodecDevice.XSTATUS_ELEMENT, "Cameras", "SpeakerTrack", "Whiteboard", "Distance");
		}

		/// <summary>
		/// Unsubscribes from the codec events.
		/// </summary>
		/// <param name="codec"></param>
		protected override void Unsubscribe(CiscoCodecDevice codec)
		{
			base.Unsubscribe(codec);

			if (codec == null)
				return;

			codec.UnregisterParserCallback(ParseCameraStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "Cameras");
			codec.UnregisterParserCallback(ParseCameraPresets, "PresetListResult");

			codec.UnregisterParserCallback(ParsePresenterTrackAvailability, CiscoCodecDevice.XSTATUS_ELEMENT, "Cameras", "PresenterTrack", "Availability");
			codec.UnregisterParserCallback(ParsePresenterTrackPresenterDetected, CiscoCodecDevice.XSTATUS_ELEMENT, "Cameras", "PresenterTrack", "PresenterDetected");
			codec.UnregisterParserCallback(ParsePresenterTrackStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "Cameras", "PresenterTrack", "Status");

			codec.UnregisterParserCallback(ParseSpeakerTrackAvailability, CiscoCodecDevice.XSTATUS_ELEMENT, "Cameras", "SpeakerTrack", "Availability");
			codec.UnregisterParserCallback(ParseSpeakerTrackStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "Cameras", "SpeakerTrack", "Status");

			codec.UnregisterParserCallback(ParseSpeakerTrackWhiteboardMode, CiscoCodecDevice.XCONFIGURATION_ELEMENT, "Cameras", "SpeakerTrack", "Whiteboard", "Mode");
			codec.UnregisterParserCallback(ParseSpeakerTrackWhiteboardDistance, CiscoCodecDevice.XSTATUS_ELEMENT, "Cameras", "SpeakerTrack", "Whiteboard", "Distance");
		}

		/// <summary>
		/// Parses the camera status result.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="resultId"></param>
		/// <param name="xml"></param>
		private void ParseCameraStatus(CiscoCodecDevice sender, string resultId, string xml)
		{
			m_CamerasSection.Enter();

			try
			{
				foreach (string child in XmlUtils.GetChildElementsAsString(xml, "Camera"))
				{
					int cameraId = XmlUtils.GetAttributeAsInt(child, "item");
					GetCamera(cameraId).Parse(child);
				}
			}
			finally
			{
				m_CamerasSection.Leave();
			}

			OnCamerasChanged.Raise(this);
		}

		/// <summary>
		/// Parses the camera presets result.
		/// </summary>
		/// <param name="codec"></param>
		/// <param name="resultid"></param>
		/// <param name="xml"></param>
		private void ParseCameraPresets(CiscoCodecDevice codec, string resultid, string xml)
		{
			IcdHashSet<int> cameras = new IcdHashSet<int>();

			m_PresetsSection.Enter();

			try
			{
				m_Presets.Clear();

				foreach (string child in XmlUtils.GetChildElementsAsString(xml))
				{
					int cameraId;
					CiscoCameraPreset preset = CameraPresetFromXml(child, out cameraId);
                                       
					m_Presets.Add(preset.PresetId, preset);

					cameras.Add(cameraId);
				}
			}
			finally
			{
				m_PresetsSection.Leave();
			}

			foreach (int camera in cameras)
				OnPresetsChanged.Raise(this, new IntEventArgs(camera));
		}

		private void ParsePresenterTrackAvailability(CiscoCodecDevice codec, string resultid, string xml)
		{
			PresenterTrackAvailability = XmlUtils.ReadElementContentAsEnum<ePresenterTrackAvailability>(xml, true);
		}

		private void ParsePresenterTrackPresenterDetected(CiscoCodecDevice codec, string resultid, string xml)
		{
			string content = XmlUtils.GetInnerXml(xml);
			PresenterDetected = bool.Parse(content);
		}

		private void ParsePresenterTrackStatus(CiscoCodecDevice codec, string resultid, string xml)
		{
			PresenterTrackMode = XmlUtils.ReadElementContentAsEnum<ePresenterTrackMode>(xml, true);
		}

		private void ParseSpeakerTrackAvailability(CiscoCodecDevice codec, string resultid, string xml)
		{
			SpeakerTrackAvailability = XmlUtils.ReadElementContentAsEnum<eSpeakerTrackAvailability>(xml, true);
		}

		private void ParseSpeakerTrackStatus(CiscoCodecDevice codec, string resultid, string xml)
		{
			SpeakerTrackStatus = XmlUtils.ReadElementContentAsEnum<eSpeakerTrackStatus>(xml, true);
		}

		private void ParseSpeakerTrackWhiteboardMode(CiscoCodecDevice codec, string resultid, string xml)
		{
			SpeakerTrackWhiteboardMode = XmlUtils.ReadElementContentAsEnum<eSpeakerTrackWhiteboardMode>(xml, true);
		}

		private void ParseSpeakerTrackWhiteboardDistance(CiscoCodecDevice codec, string resultid, string xml)
		{
			SpeakerTrackWhiteboardDistance = XmlUtils.ReadElementContentAsInt(xml);
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

			addRow("PresenterTrack Availability", PresenterTrackAvailability);
			addRow("PresenterTrack Presenter Detected", PresenterDetected);
			addRow("PresenterTrack Mode", PresenterTrackMode);
			addRow("SpeakerTrack Availability", SpeakerTrackAvailability);
			addRow("SpeakerTrack Status", SpeakerTrackStatus);
			addRow("SpeakerTrack Whiteboard Mode", SpeakerTrackWhiteboardMode);
			addRow("SpeakerTrack Whiteboard Distance", SpeakerTrackWhiteboardDistance);
		}

		/// <summary>
		/// Gets the child console node groups.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase group in GetBaseConsoleNodes())
				yield return group;

			yield return ConsoleNodeGroup.KeyNodeMap("Cameras", "The collection of near cameras", GetCameras(), c => (uint)c.CameraId);
		}

		/// <summary>
		/// Shim to avoid "unverifiable code" warning.
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

			string presenterTrackModeHelp =
				string.Format("SetPresenterTrackMode <{0}>",
				              StringUtils.ArrayFormat(EnumUtils.GetValues<ePresenterTrackMode>()));

			yield return new GenericConsoleCommand<ePresenterTrackMode>("SetPresenterTrackMode",
			                                                            presenterTrackModeHelp, m => SetPresenterTrackMode(m));

			yield return new ConsoleCommand("ActivateSpeakerTrack", "Activates the SpeakerTrack", () => ActivateSpeakerTrack());
			yield return new ConsoleCommand("DeactivateSpeakerTrack", "Deactivates the SpeakerTrack", () => DeactivateSpeakerTrack());

			string speakerTrackWhiteboardModeHelp =
				string.Format("SetSpeakerTrackWhiteboardMode <{0}>",
				              StringUtils.ArrayFormat(EnumUtils.GetValues<eSpeakerTrackWhiteboardMode>()));

			yield return new GenericConsoleCommand<eSpeakerTrackWhiteboardMode>("SetSpeakerTrackWhiteboardMode",
			                                                                    speakerTrackWhiteboardModeHelp,
			                                                                    m => SetSpeakerTrackWhiteboardMode(m));

			yield return new GenericConsoleCommand<ushort>("SetSpeakerTrackWhiteboardDistance",
			                                               "SetSpeakerTrackWhiteboardDistance <Centimeters>",
			                                               i => SetSpeakerTrackWhiteboardDistance(i));

			yield return new GenericConsoleCommand<int, ushort>("AlignSpeakerTrackWhiteboardPosition",
			                                                    "AlignSpeakerTrackWhiteboardPosition <CameraId, Centimeters>",
			                                                    (c, d) => AlignSpeakerTrackWhiteboardPosition(c, d));

			yield return new GenericConsoleCommand<int>("StoreSpeakerTrackWhiteboardPosition",
														"StoreSpeakerTrackWhiteboardPosition <CameraId>",
														i => StoreSpeakerTrackWhiteboardPosition(i));

			yield return new GenericConsoleCommand<int>("ActivateSpeakerTrackWhiteboardPosition",
			                                            "ActivateSpeakerTrackWhiteboardPosition <CameraId>",
			                                            i => ActivateSpeakerTrackWhiteboardPosition(i));

			yield return new ConsoleCommand("RemovePresets",
			                                "Removes all camera presets",
			                                () => RemovePresets());
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
	}
}
