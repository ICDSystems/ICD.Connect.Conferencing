using System;
using ICD.Common.Utils;
using ICD.Common.Utils.Xml;

namespace ICD.Connect.Conferencing.Cisco.Components.Presentation
{
	/// <summary>
	/// Represents a Cisco presentation.
	/// </summary>
	public struct PresentationItem
	{
		// Ignore missing comment warnings.
#pragma warning disable 1591
		public enum eSendingMode
		{
			Off,
			LocalRemote,
			LocalOnly
		}
#pragma warning restore 1591

		private readonly eSendingMode m_SendingMode;
		private readonly int m_VideoInputConnector;

		/// <summary>
		/// Gets the presentation sending mode.
		/// </summary>
		public eSendingMode SendingMode { get { return m_SendingMode; } }

		/// <summary>
		/// Gets the video connector source.
		/// </summary>
		public int VideoInputConnector { get { return m_VideoInputConnector; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="sendingMode"></param>
		/// <param name="videoInputConnector"></param>
		public PresentationItem(eSendingMode sendingMode, int videoInputConnector)
		{
			m_SendingMode = sendingMode;
			m_VideoInputConnector = videoInputConnector;
		}

		/// <summary>
		/// Instantiates a Presentation from a LocalInstance xml element.
		/// </summary>
		/// <param name="localInstance"></param>
		/// <returns></returns>
		public static PresentationItem FromLocalInstance(IcdXmlReader localInstance)
		{
			int source = 0;
			eSendingMode sendingMode = eSendingMode.Off;

			foreach (IcdXmlReader child in localInstance.GetChildElements())
			{
				switch (child.Name)
				{
					case "SendingMode":
						sendingMode = EnumUtils.Parse<eSendingMode>(child.ReadElementContentAsString(), true);
						break;

					case "Source":
						source = child.ReadElementContentAsInt();
						break;

					default:
						throw new FormatException();
				}

				child.Dispose();
			}

			return new PresentationItem(sendingMode, source);
		}
	}
}
