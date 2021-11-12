using ICD.Common.Utils.Collections;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Utils
{
	public sealed class CiscoCallbackNode
	{
		private readonly Dictionary<string, CiscoCallbackNode> m_Children;
		private readonly IcdHashSet<CiscoCodecDevice.ParserCallback> m_Callbacks;
		private readonly SafeCriticalSection m_Section;

		public CiscoCallbackNode()
        {
			m_Children = new Dictionary<string, CiscoCallbackNode>();
			m_Callbacks = new IcdHashSet<CiscoCodecDevice.ParserCallback>();
			m_Section = new SafeCriticalSection();
        }

		public void RegisterCallback(CiscoCodecDevice.ParserCallback callback, string[] path)
		{
			m_Section.Enter();

			try
			{
				if (path.Length == 0)
					m_Callbacks.Add(callback);
				else
					m_Children.GetOrAddNew(path[0]).RegisterCallback(callback, path.Skip(1).ToArray());
			}
			finally
			{
				m_Section.Leave();
			}
		}

		public void UnregisterCallback(CiscoCodecDevice.ParserCallback callback, string[] path)
		{
			m_Section.Enter();

			try
			{
				// TODO - Remove nodes that don't exist anymore
				if (path.Length == 0)
					m_Callbacks.Remove(callback);
				else
					m_Children.GetOrAddNew(path[0]).UnregisterCallback(callback, path.Skip(1).ToArray());
			}
			finally
			{
				m_Section.Leave();
			}
		}

		public IEnumerable<CiscoCodecDevice.ParserCallback> GetCallbacks(string[] path)
		{
			m_Section.Enter();

			try
			{
				if (path.Length == 0)
					return m_Callbacks.ToArray();
				else
				{
					CiscoCallbackNode child = m_Children.GetDefault(path[0]);
					return child == null
						       ? Enumerable.Empty<CiscoCodecDevice.ParserCallback>()
						       : child.GetCallbacks(path.Skip(1).ToArray());
				}
			}
			finally
			{
				m_Section.Leave();
			}
		}

		public IEnumerable<CiscoCodecDevice.ParserCallback> GetCallbacks()
		{
			return m_Section.Execute(() => m_Callbacks.ToArray());
		}

		public IEnumerable<string[]> GetPathsRecursive()
		{
			m_Section.Enter();

			try
			{
				List<string[]> output = new List<string[]>();

				// Add children that have callbacks
				foreach (KeyValuePair<string, CiscoCallbackNode> kvp in m_Children)
				{
					if (kvp.Value.GetCallbacks().Any())
						output.Add(new[] {kvp.Key});
				}

				// Recurse
				foreach (KeyValuePair<string, CiscoCallbackNode> kvp in m_Children)
				{
					IEnumerable<string[]> nested =
						kvp.Value
						   .GetPathsRecursive()
						   .Select(p => p.Prepend(kvp.Key).ToArray());
					output.AddRange(nested);
				}

				return output;
			}
			finally
			{
				m_Section.Leave();
			}
		}

		public CiscoCallbackNode GetChild(string[] path)
		{
			if (path.Length == 0)
				return this;

			CiscoCallbackNode child = m_Section.Execute(() => m_Children.GetDefault(path[0]));
			if (child == null)
				return null;

			return child.GetChild(path.Skip(1).ToArray());
		}

		public IEnumerable<CiscoCallbackNode> GetChildren()
		{
			return m_Section.Execute(() => m_Children.Values.ToArray());
		}
	}
}