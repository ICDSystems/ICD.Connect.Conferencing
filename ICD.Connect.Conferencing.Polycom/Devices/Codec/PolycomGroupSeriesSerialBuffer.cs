using System;
using System.Collections.Generic;
using System.Text;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.SerialBuffers;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec
{
	public sealed class PolycomGroupSeriesSerialBuffer : ISerialBuffer
	{
		/// <summary>
		/// Raised when a complete message has been buffered.
		/// </summary>
		public event EventHandler<StringEventArgs> OnCompletedSerial;

		/// <summary>
		/// Raised when a username prompt has been buffered.
		/// </summary>
		public event EventHandler OnUsernamePrompt;

		/// <summary>
		/// Raised when a password prompt has been buffered.
		/// </summary>
		public event EventHandler OnPasswordPrompt; 

		private readonly StringBuilder m_RxData;
		private readonly Queue<string> m_Queue;

		private readonly SafeCriticalSection m_QueueSection;
		private readonly SafeCriticalSection m_ParseSection;

		private static readonly char[] s_Delimiters =
		{
			'\r',
			'\n',
			':' // Polycom doesn't use CRLF after login prompts
		};

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		public PolycomGroupSeriesSerialBuffer()
		{
			m_RxData = new StringBuilder();
			m_Queue = new Queue<string>();

			m_QueueSection = new SafeCriticalSection();
			m_ParseSection = new SafeCriticalSection();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Enqueues the serial data.
		/// </summary>
		/// <param name="data"></param>
		public void Enqueue(string data)
		{
			m_QueueSection.Execute(() => m_Queue.Enqueue(data));
			Parse();
		}

		/// <summary>
		/// Clears all queued data in the buffer.
		/// </summary>
		public void Clear()
		{
			m_ParseSection.Enter();
			m_QueueSection.Enter();

			try
			{
				m_RxData.Clear();
				m_Queue.Clear();
			}
			finally
			{
				m_ParseSection.Leave();
				m_QueueSection.Leave();
			}
		}

		#endregion

		/// <summary>
		/// Searches the enqueued serial data for the delimiter character.
		/// Complete strings are raised via the OnCompletedString event.
		/// </summary>
		private void Parse()
		{
			if (!m_ParseSection.TryEnter())
				return;

			try
			{
				string data = null;

				while (m_QueueSection.Execute(() => m_Queue.Dequeue(out data)))
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

						switch (delimiter)
						{
							// Login prompt
							case ':':
								string prompt = m_RxData.ToString().ToLower();

								switch (prompt)
								{
									case "password":
										m_RxData.Clear();
										OnPasswordPrompt.Raise(this);
										continue;

									case "username":
										m_RxData.Clear();
										OnUsernamePrompt.Raise(this);
										continue;

									// Unhandled colon, push it back onto the rx data
									default:
										m_RxData.Append(':');
										continue;
								}
						}

						string output = m_RxData.Pop();
						if (string.IsNullOrEmpty(output))
							continue;

						OnCompletedSerial.Raise(this, new StringEventArgs(output));
						break;
					}
				}
			}
			finally
			{
				m_ParseSection.Leave();
			}
		}
	}
}
