using System;
using System.Collections.Generic;
using System.Text;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.SerialBuffers;

namespace ICD.Connect.Conferencing.Vaddio.Devices.AvBridge
{
	public sealed class VaddioAvBridgeSerialBuffer : AbstractSerialBuffer
	{
		private static readonly char[] s_Delimiters =
		{
			'>',
			':'
		};

		/// <summary>
		/// Raised when a username prompt has been buffered.
		/// </summary>
		public event EventHandler OnUsernamePrompt;

		/// <summary>
		/// Raised when a password prompt has been buffered.
		/// </summary>
		public event EventHandler OnPasswordPrompt;

		/// <summary>
		/// Raised when a welcome prompt has been buffered.
		/// </summary>
		public event EventHandler OnWelcomePrompt;

		private readonly StringBuilder m_RxData;

		public VaddioAvBridgeSerialBuffer()
		{
			m_RxData = new StringBuilder();
		}

		/// <summary>
		/// Override to clear any current state.
		/// </summary>
		protected override void ClearFinal()
		{
			m_RxData.Clear();
		}

		/// <summary>
		/// Override to process the given item for chunking.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		protected override IEnumerable<string> Process(string data)
		{
			while (true)
			{
				int index = data.IndexOfAny(s_Delimiters);
				if (index < 0)
				{
					m_RxData.Append(data);
					break;
				}

				char delimiter = data[index];

				m_RxData.Append(data.Substring(0, index));
				data = data.Substring(index + 1);

				// Scrub the vaddio delimiter - we're delimiting on >
				const string vaddioDelimiter = "\x1B\x5B\x4A";
				m_RxData.Replace(vaddioDelimiter, "");

				string prompt = m_RxData.ToString().ToLower();
				switch (delimiter)
				{
					// Login prompt
					case ':':
						if (prompt.Contains("login") && !prompt.Contains("last login"))
						{
							m_RxData.Clear();
							OnUsernamePrompt.Raise(this);
							break;
						}

						if (prompt.Contains("password"))
						{
							m_RxData.Clear();
							OnPasswordPrompt.Raise(this);
							break;
						}
						
						// Unhandled colon, push it back onto the rx data
						m_RxData.Append(':');
						continue;

						// Welcome prompt used to initialize
					case '>':
						if (prompt.Contains("vaddio interactive shell"))
						{
							m_RxData.Clear();
							OnWelcomePrompt.Raise(this);
							continue;
						}
						break;
				}

				string output = m_RxData.Pop();
				if (!string.IsNullOrEmpty(output))
					yield return output;
			}
		}
	}
}
