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
		private readonly Dictionary<int, CameraPreset> m_Presets;

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
			m_Presets = new Dictionary<int, CameraPreset>();

			m_CamerasSection = new SafeCriticalSection();
			m_PresetsSection = new SafeCriticalSection();

			Subscribe(Codec);

			if (Codec.Initialized)
				Initialize();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Gets the camera with the given id.
		/// </summary>
		/// <param name="cameraId"></param>
		/// <returns></returns>
		[PublicAPI]
		public NearCamera GetCamera(int cameraId)
		{
			return m_CamerasSection.Execute(() => m_Cameras.ContainsKey(cameraId) ? m_Cameras[cameraId] : null);
		}

		/// <summary>
		/// Gets all of the available cameras.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		public IEnumerable<NearCamera> GetCameras()
		{
			return m_CamerasSection.Execute(() => m_Cameras.OrderBy(p => p.Key).Select(p => p.Value).ToArray());
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
			return m_PresetsSection.Execute(() => m_Presets.OrderBy(p => p.Value.ListPosition).Select(p => p.Value).ToArray());
		}

		/// <summary>
		/// Gets the camera presets for the given camera.
		/// </summary>
		/// <param name="cameraId"></param>
		/// <returns></returns>
		[PublicAPI]
		public CameraPreset[] GetCameraPresets(int cameraId)
		{
			return GetCameraPresets().Where(p => p.CameraId == cameraId).ToArray();
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
					CameraPreset preset = CameraPreset.FromXml(child);
					m_Presets[preset.PresetId] = preset;
				}
			}
			finally
			{
				m_PresetsSection.Leave();
			}

			OnPresetsChanged.Raise(this);
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
