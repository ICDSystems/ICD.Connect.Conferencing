using System;
using ICD.Common.Properties;
using ICD.Common.Utils.Xml;

namespace ICD.Connect.Conferencing.Cisco.Components.Cameras
{
	/// <summary>
	/// CameraPreset provides information about a near camera preset.
	/// </summary>
	public struct CameraPreset
	{
		private readonly int m_PresetId;
		private readonly int m_CameraId;
		private readonly int m_ListPosition;
		private readonly string m_Name;

		#region Region Properties

		/// <summary>
		/// Gets the preset id.
		/// </summary>
		public int PresetId { get { return m_PresetId; } }

		/// <summary>
		/// Gets the camera id.
		/// </summary>
		public int CameraId { get { return m_CameraId; } }

		/// <summary>
		/// Gets the list position.
		/// </summary>
		public int ListPosition { get { return m_ListPosition; } }

		/// <summary>
		/// Gets the name.
		/// </summary>
		[PublicAPI]
		public string Name { get { return m_Name; } }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="presetId"></param>
		/// <param name="cameraId"></param>
		/// <param name="listPosition"></param>
		/// <param name="name"></param>
		public CameraPreset(int presetId, int cameraId, int listPosition, string name)
		{
			m_PresetId = presetId;
			m_CameraId = cameraId;
			m_ListPosition = listPosition;
			m_Name = name;
		}

		/// <summary>
		/// Instantiates a camera preset from a Preset element.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		public static CameraPreset FromXml(string xml)
		{
			using (IcdXmlReader reader = new IcdXmlReader(xml))
			{
				reader.SkipToNextElement();

				int listPosition = reader.GetAttributeAsInt("item");
				int presetId = 0;
				int cameraId = 0;
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

				return new CameraPreset(presetId, cameraId, listPosition, name);
			}
		}

		#endregion
	}
}
