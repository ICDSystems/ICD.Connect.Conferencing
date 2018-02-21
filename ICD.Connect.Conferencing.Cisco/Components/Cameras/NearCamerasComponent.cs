using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Xml;
using ICD.Connect.API.Nodes;
using ICD.Connect.Cameras;

namespace ICD.Connect.Conferencing.Cisco.Components.Cameras
{
	/// <summary>
	/// NearCamerasComponent provides methods for managing the near cameras.
	/// </summary>
	public sealed class NearCamerasComponent : AbstractCiscoComponent
	{
		/// <summary>
		/// Called when the cameras are rebuilt.
		/// </summary>
		public event EventHandler OnCamerasChanged;

		/// <summary>
		/// Called when the presets are rebuilt.
		/// </summary>
		public event EventHandler OnPresetsChanged;

		private readonly Dictionary<int, NearCamera> m_Cameras;
		private readonly Dictionary<int, Dictionary<int, CameraPreset>> m_Presets;

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
		public NearCamerasComponent(CiscoCodec codec) : base(codec)
		{
			m_Cameras = new Dictionary<int, NearCamera>();
			m_Presets = new Dictionary<int, Dictionary<int, CameraPreset>>();

			m_CamerasSection = new SafeCriticalSection();
			m_PresetsSection = new SafeCriticalSection();

			Subscribe(Codec);

			if (Codec.Initialized)
				Initialize();
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
				if (!m_Cameras.ContainsKey(cameraId))
					m_Cameras[cameraId] = new NearCamera(cameraId, Codec);
				return m_Cameras[cameraId];
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
			return m_CamerasSection.Execute(() => m_Cameras.Select(p => p.Value));
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
			return m_PresetsSection.Execute(() => m_Presets.SelectMany(kvp => kvp.Value).Select(kvp => kvp.Value));
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
				return !m_Presets.ContainsKey(cameraId)
					? Enumerable.Empty<CameraPreset>()
					: m_Presets[cameraId].Values.ToArray();
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
		protected override void Subscribe(CiscoCodec codec)
		{
			base.Subscribe(codec);

			if (codec == null)
				return;

			codec.RegisterParserCallback(ParseCameraStatus, CiscoCodec.XSTATUS_ELEMENT, "Cameras");
			codec.RegisterParserCallback(ParseCameraPresets, "PresetListResult");
		}

		/// <summary>
		/// Unsubscribes from the codec events.
		/// </summary>
		/// <param name="codec"></param>
		protected override void Unsubscribe(CiscoCodec codec)
		{
			base.Unsubscribe(codec);

			if (codec == null)
				return;

			codec.UnregisterParserCallback(ParseCameraStatus, CiscoCodec.XSTATUS_ELEMENT, "Cameras");
			codec.UnregisterParserCallback(ParseCameraPresets, "PresetListResult");
		}

		private void ParseCameraStatus(CiscoCodec sender, string resultId, string xml)
		{
			m_CamerasSection.Enter();

			try
			{
				foreach (string child in XmlUtils.GetChildElementsAsString(xml, "Camera"))
				{
					int cameraId = XmlUtils.GetAttributeAsInt(child, "item");

					if (!m_Cameras.ContainsKey(cameraId))
						m_Cameras[cameraId] = new NearCamera(cameraId, Codec);
					m_Cameras[cameraId].Parse(child);
				}
			}
			finally
			{
				m_CamerasSection.Leave();
			}

			OnCamerasChanged.Raise(this);
		}

		private void ParseCameraPresets(CiscoCodec codec, string resultid, string xml)
		{
			m_PresetsSection.Enter();

			try
			{
				m_Presets.Clear();

				foreach (string child in XmlUtils.GetChildElementsAsString(xml))
				{
					int cameraId;
					CameraPreset preset = CameraPresetFromXml(child, out cameraId);

					if (!m_Presets.ContainsKey(cameraId))
						m_Presets[cameraId] = new Dictionary<int, CameraPreset>();
					m_Presets[cameraId][preset.PresetId] = preset;
				}
			}
			finally
			{
				m_PresetsSection.Leave();
			}

			OnPresetsChanged.Raise(this);
		}

		/// <summary>
		/// Instantiates a camera preset from a Preset element.
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="cameraId"></param>
		/// <returns></returns>
		public static CameraPreset CameraPresetFromXml(string xml, out int cameraId)
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

			NearCamera[] cameras = m_CamerasSection.Execute(() => m_Cameras.Values.ToArray());
			yield return ConsoleNodeGroup.KeyNodeMap("Cameras", "The collection of near cameras", cameras, c => (uint)c.CameraId);
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
