using System;
using System.Collections.Generic;
using System.Linq;

namespace ICD.Connect.Conferencing.Vaddio.Devices.AvBridge
{
	public sealed class VaddioAvBridgeSerialResponse
	{
		private string m_Command;
		private string m_CommandSetValue;
		private string m_Option;
		private string m_OptionValue;
		private string m_StatusCode;

		#region Properties

		public string Command
		{
			get { return m_Command; }
		}

		public string CommandSetValue
		{
			get { return m_CommandSetValue; }
		}

		public string Option
		{
			get { return m_Option; }
		}

		public string OptionValue
		{
			get { return m_OptionValue; }
		}

		public string StatusCode
		{
			get { return m_StatusCode; }
		}

		#endregion

		#region Constructor

		public VaddioAvBridgeSerialResponse(string serialData)
		{
			Parse(serialData);
		}

		#endregion

		#region Private Methods

		private void Parse(string serialData)
		{
			string[] content = serialData.Split('\n');

			// Scrub delimiters from the data.
			for (int i = 0; i < content.Length; i++)
			{
				content[i] = content[i].Replace("\r", "");
				content[i] = content[i].Trim();
				if (string.IsNullOrEmpty(content[i]))
					content = content.Where((source, index) => index != i).ToArray();
			}

			KeyValuePair<string, string> commandAndValue;
			switch (content.Length)
			{
				// Junk data that sometimes appears between commands.
				case 1:
					m_Command = null;
					m_CommandSetValue = null;
					m_Option = null;
					m_OptionValue = null;
					m_StatusCode = null;
					break;
				// No options in the response.
				case 2:
					commandAndValue = SplitCommandFromSetValue(content[0]);
					m_Command = commandAndValue.Key;
					m_CommandSetValue = commandAndValue.Value;
					m_Option = null;
					m_OptionValue = null;
					m_StatusCode = content[1];
					break;
				// Options in the response.
				case 3:
					commandAndValue = SplitCommandFromSetValue(content[0]);
					var optionAndValue = SplitOptionFromValue(content[1]);

					m_Command = commandAndValue.Key;
					m_CommandSetValue = commandAndValue.Value;
					m_Option = optionAndValue.Key;
					m_OptionValue = optionAndValue.Value;
					m_StatusCode = content[2];
					break;

				default:
					throw new InvalidOperationException("Cannot parse AV Bridge response, invalid number of response lines");
			}
		}

		private static KeyValuePair<string, string> SplitCommandFromSetValue(string command)
		{
			var content = command.Split(' ');
			if (content.Length == 3)
				return new KeyValuePair<string, string>(string.Join(" ", content, 0, 2), content[2]);
			if (content.Length == 4)
			{
				return new KeyValuePair<string, string>(string.Join(" ", content, 0, 3), content[3]);
			}

			throw new InvalidOperationException("Cannot parse AV Bridge command value, invalid command length");
		}

		private static KeyValuePair<string, string> SplitOptionFromValue(string option)
		{
			var content = option.Split(':');

			// Scrub whitespace.
			for (int i = 0; i < content.Length; i++)
				content[i] = content[i].Replace(" ", "");

			return new KeyValuePair<string, string>(content[0], content[1]);
		}

		#endregion
	}
}
