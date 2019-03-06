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
using ICD.Connect.Cameras;

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

		private readonly IcdOrderedDictionary<int, NearCamera> m_Cameras;

		// CameraId -> PresetId -> Preset
		private readonly IcdOrderedDictionary<int, IcdOrderedDictionary<int, CameraPreset>> m_CameraToPresets;
		private readonly IcdOrderedDictionary<int, CameraPreset> m_Presets;

		private readonly SafeCriticalSection m_CamerasSection;
		private readonly SafeCriticalSection m_PresetsSection;

		private ePresenterTrackMode m_PresenterTrackMode;
		private ePresenterTrackAvailability m_PresenterTrackAvailability;
		private bool m_PresenterDetected;

		private eSpeakerTrackAvailability m_SpeakerTrackAvailability;
		private eSpeakerTrackStatus m_SpeakerTrackStatus;
		private eSpeakerTrackWhiteboardMode m_SpeakerTrackWhiteboardMode;

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

				Codec.Log(eSeverity.Informational, "PresenterTrack availability is {0}", m_PresenterTrackAvailability);

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

				Codec.Log(eSeverity.Informational, "PresenterTrack detected is {0}", m_PresenterDetected);

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

				Codec.Log(eSeverity.Informational, "PresenterTrack mode is {0}", m_PresenterTrackMode);

				OnPresenterTrackModeChanged.Raise(this, new PresenterTrackModeEventArgs(m_PresenterTrackMode));
			}
		}

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

				Codec.Log(eSeverity.Informational, "SpeakerTrack availability is {0}", m_SpeakerTrackAvailability);

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

				Codec.Log(eSeverity.Informational, "SpeakerTrack status is {0}", m_SpeakerTrackStatus);

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

				Codec.Log(eSeverity.Informational, "SpeakerTrack Whiteboard Mode is {0}", m_SpeakerTrackWhiteboardMode);

				OnSpeakerTrackWhiteboardModeChanged.Raise(this, new SpeakerTrackWhiteboardModeEventArgs(m_SpeakerTrackWhiteboardMode));
			}
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="codec"></param>
		public NearCamerasComponent(CiscoCodecDevice codec) : base(codec)
		{
			m_Cameras = new IcdOrderedDictionary<int, NearCamera>();
			m_CameraToPresets = new IcdOrderedDictionary<int, IcdOrderedDictionary<int, CameraPreset>>();
			m_Presets = new IcdOrderedDictionary<int, CameraPreset>();

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
				NearCamera camera;
				if (!m_Cameras.TryGetValue(cameraId, out camera))
				{
					camera = new NearCamera(cameraId, Codec);
					m_Cameras[cameraId] = camera;
				}
				return camera;
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

		/// <summary>
		/// Gets the camera presets.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		public IEnumerable<CameraPreset> GetCameraPresets()
		{
			return m_PresetsSection.Execute(() => m_Presets.Values.ToArray());
		}

		/// <summary>
		/// Gets the camera presets for the given camera.
		/// </summary>
		/// <param name="cameraId"></param>
		/// <returns></returns>
		[PublicAPI]
		public IEnumerable<CameraPreset> GetCameraPresets(int cameraId)
		{
			m_PresetsSection.Enter();

			try
			{
				IcdOrderedDictionary<int, CameraPreset> presetMap;
				return m_CameraToPresets.TryGetValue(cameraId, out presetMap)
					? presetMap.Values.ToArray()
					: Enumerable.Empty<CameraPreset>();
			}
			finally
			{
				m_PresetsSection.Leave();
			}
		}

		/// <summary>
		/// Gets the presets for the given camera id and remaps them starting from preset id 1.
		/// </summary>
		/// <param name="cameraId"></param>
		/// <returns></returns>
		public IEnumerable<CameraPreset> GetRemappedCameraPresets(int cameraId)
		{
			int newId = 1;
			return GetCameraPresets(cameraId).OrderBy(p => p.PresetId)
			                                 .Select(p => new CameraPreset(newId++, p.Name));
		}

		/// <summary>
		/// Looks up the preset id for the camera id and preset index and activates it.
		/// </summary>
		/// <param name="cameraId"></param>
		/// <param name="presetIndex"></param>
		public void ActivatePreset(int cameraId, int presetIndex)
		{
			int presetId = GetPresetId(cameraId, presetIndex);
			ActivatePresetById(presetId);
		}

		/// <summary>
		/// Activates the preset with the given id.
		/// </summary>
		/// <param name="presetId"></param>
		public void ActivatePresetById(int presetId)
		{
			Codec.SendCommand("xCommand Camera Preset Activate PresetId: {0}", presetId);
			Codec.Log(eSeverity.Informational, "Activating Preset {0}", presetId);
		}

		/// <summary>
		/// Looks up the preset id for the camera id and preset index and stores it.
		/// </summary>
		/// <param name="cameraId"></param>
		/// <param name="presetIndex"></param>
		public void StorePreset(int cameraId, int presetIndex)
		{
			int presetId = GetPresetId(cameraId, presetIndex);
			StorePresetById(cameraId, presetId);
		}

		/// <summary>
		/// Stores the given camera position as the preset with the given id.
		/// </summary>
		/// <param name="cameraId"></param>
		/// <param name="presetId"></param>
		public void StorePresetById(int cameraId, int presetId)
		{
			Codec.SendCommand("xCommand Camera Preset Store CameraId: {0} PresetId: \"{1}\"", cameraId, presetId);
			Codec.Log(eSeverity.Informational, "Storing preset {0} for Camera {1}", presetId, cameraId);

			// Updates the presets
			Codec.SendCommand("xCommand Camera Preset List");
		}

		/// <summary>
		/// Sets the presenter track mode.
		/// </summary>
		/// <param name="mode"></param>
		[PublicAPI]
		public void SetPresenterTrackMode(ePresenterTrackMode mode)
		{
			Codec.SendCommand("xCommand Cameras PresenterTrack Set Mode: {0}", mode);
			Codec.Log(eSeverity.Informational, "Setting PresenterTrack mode to {0}", mode);
		}

		/// <summary>
		/// Activates the speaker track.
		/// </summary>
		[PublicAPI]
		public void ActivateSpeakerTrack()
		{
			Codec.SendCommand("xCommand Cameras SpeakerTrack Activate");
			Codec.Log(eSeverity.Informational, "Setting SpeakerTrack active");
		}

		/// <summary>
		/// Deactivates the speaker track.
		/// </summary>
		[PublicAPI]
		public void DeactivateSpeakerTrack()
		{
			Codec.SendCommand("xCommand Cameras SpeakerTrack Deactivate");
			Codec.Log(eSeverity.Informational, "Setting SpeakerTrack inactive");
		}

		/// <summary>
		/// Sets the SpeakerTrack Whiteboard Mode.
		/// </summary>
		/// <param name="mode"></param>
		[PublicAPI]
		public void SetSpeakerTrackWhiteboardMode(eSpeakerTrackWhiteboardMode mode)
		{
			Codec.SendCommand("xCommand Cameras SpeakerTrack Whiteboard Mode: {0}", mode);
			Codec.Log(eSeverity.Informational, "Setting SpeakerTrack {0}", mode);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Gets the existing preset id reserved for this camera at the given preset index,
		/// or generates a new one if none has been assigned.
		/// </summary>
		/// <param name="cameraId"></param>
		/// <param name="presetIndex"></param>
		/// <returns></returns>
		private int GetPresetId(int cameraId, int presetIndex)
		{
			m_PresetsSection.Enter();

			try
			{
				IcdOrderedDictionary<int, CameraPreset> presetsMap;
				int id;

				if (m_CameraToPresets.TryGetValue(cameraId, out presetsMap) &&
				    presetsMap.Keys.TryElementAt(presetIndex, out id))
					return id;

				return GetNextPresetId();
			}
			finally
			{
				m_PresetsSection.Leave();
			}
		}

		private int GetNextPresetId()
		{
			m_PresetsSection.Enter();

			try
			{
				return Enumerable.Range(1, int.MaxValue).First(i => !m_Presets.ContainsKey(i));
			}
			finally
			{
				m_PresetsSection.Leave();
			}
		}

		/// <summary>
		/// Instantiates a camera preset from a Preset element.
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="cameraId"></param>
		/// <returns></returns>
		private static CameraPreset CameraPresetFromXml(string xml, out int cameraId)
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

				return new CameraPreset(presetId, name);
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
			Codec.SendCommand("xCommand Camera Preset List");
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

			codec.RegisterParserCallback(ParseSpeakerTrackWhiteboardMode, CiscoCodecDevice.XCONFIGURATION_ELEMENT, "Cameras", "SpeakerTrack", "Whiteboard", "Mode");
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
				m_CameraToPresets.Clear();
				m_Presets.Clear();

				foreach (string child in XmlUtils.GetChildElementsAsString(xml))
				{
					int cameraId;
					CameraPreset preset = CameraPresetFromXml(child, out cameraId);

					IcdOrderedDictionary<int, CameraPreset> cameraPresetsMap;
					if (!m_CameraToPresets.TryGetValue(cameraId, out cameraPresetsMap))
					{
						cameraPresetsMap = new IcdOrderedDictionary<int, CameraPreset>();
						m_CameraToPresets.Add(cameraId, cameraPresetsMap);
					}

					cameraPresetsMap.Add(preset.PresetId, preset);
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

			string presenterTrackModeHelp = string.Format("SetPresenterTrackMode <{0}>",
			                                              StringUtils.ArrayFormat(EnumUtils.GetValues<ePresenterTrackMode>()));

			yield return new GenericConsoleCommand<ePresenterTrackMode>("SetPresenterTrackMode",
			                                                            presenterTrackModeHelp, m => SetPresenterTrackMode(m));

			yield return new ConsoleCommand("ActivateSpeakerTrack", "Activates the SpeakerTrack", () => ActivateSpeakerTrack());
			yield return new ConsoleCommand("DeactivateSpeakerTrack", "Deactivates the SpeakerTrack", () => DeactivateSpeakerTrack());

			string speakerTrackWhiteboardMode = string.Format("SetSpeakerTrackWhiteboardMode <{0}>",
															  StringUtils.ArrayFormat(EnumUtils.GetValues<eSpeakerTrackWhiteboardMode>()));

			yield return new GenericConsoleCommand<eSpeakerTrackWhiteboardMode>("SetSpeakerTrackWhiteboardMode",
			                                                                    speakerTrackWhiteboardMode,
			                                                                    m => SetSpeakerTrackWhiteboardMode(m));
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
