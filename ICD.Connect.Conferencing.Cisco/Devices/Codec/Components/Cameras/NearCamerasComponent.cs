using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Xml;
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
		/// Raises when the cameras are rebuilt.
		/// </summary>
		public event EventHandler OnCamerasChanged;

		/// <summary>
		/// Raises the camera id when presets change.
		/// </summary>
		public event EventHandler<IntEventArgs> OnPresetsChanged;

		private readonly IcdOrderedDictionary<int, NearCamera> m_Cameras;

		// CameraId -> PresetId -> Preset
		private readonly IcdOrderedDictionary<int, IcdOrderedDictionary<int, CameraPreset>> m_Presets;

		private readonly SafeCriticalSection m_CamerasSection;
		private readonly SafeCriticalSection m_PresetsSection;

		/// <summary>
		/// Gets the number of cameras.
		/// </summary>
		[PublicAPI]
		public int CamerasCount { get { return m_Cameras.Count; } }

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="codec"></param>
		public NearCamerasComponent(CiscoCodecDevice codec) : base(codec)
		{
			m_Cameras = new IcdOrderedDictionary<int, NearCamera>();
			m_Presets = new IcdOrderedDictionary<int, IcdOrderedDictionary<int, CameraPreset>>();

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
			return m_PresetsSection.Execute(() => m_Presets.SelectMany(kvp => kvp.Value).Select(kvp => kvp.Value).ToArray());
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
				return m_Presets.TryGetValue(cameraId, out presetMap)
					? presetMap.Values.ToArray()
					: Enumerable.Empty<CameraPreset>();
			}
			finally
			{
				m_PresetsSection.Leave();
			}
		}

		#endregion

		#region Private Methods

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
					CameraPreset preset = CameraPresetFromXml(child, out cameraId);

					IcdOrderedDictionary<int, CameraPreset> cameraPresetsMap;
					if (!m_Presets.TryGetValue(cameraId, out cameraPresetsMap))
					{
						cameraPresetsMap = new IcdOrderedDictionary<int, CameraPreset>();
						m_Presets.Add(cameraId, cameraPresetsMap);
					}

					cameraPresetsMap.Add(preset.PresetId, preset);

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

						default:
							throw new ArgumentException("Unknown element: " + child.Name);
					}

					child.Dispose();
				}

				return new CameraPreset(presetId, name);
			}
		}

		#endregion

		#region Console

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

		#endregion
	}
}
