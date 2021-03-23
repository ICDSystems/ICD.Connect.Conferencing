using System.Text.RegularExpressions;
using ICD.Common.Properties;
using ICD.Common.Utils;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses
{
	public abstract class AbstractAudioInputOutputLine
	{
		private const string NAME_REGEX = @"^(?'device'.*) \((?'name'.*)\)$";

		[CanBeNull]
		public string Alias { get; set; }

		[CanBeNull]
		public string Name { get; set; }

		public bool? Selected { get; set; }

		public bool? CombinedDevice { get; set; }

		[CanBeNull]
		public string Id { get; set; }

		public bool? ManuallySelected { get; set; }

		public int? NumberOfCombinedDevices { get; set; }

		public int? PtzComId { get; set; }

		public string DeviceType
		{
			get
			{
				if (Name == null)
					return null;

				Match match;
				return RegexUtils.Matches(Name, NAME_REGEX, out match) ? match.Groups["device"].Value : null;
			}
		}

		public string ShortName
		{
			get
			{
				if (Name == null)
					return null;

				Match match;
				return RegexUtils.Matches(Name, NAME_REGEX, out match) ? match.Groups["name"].Value : null;
			}
		}
	}
}
