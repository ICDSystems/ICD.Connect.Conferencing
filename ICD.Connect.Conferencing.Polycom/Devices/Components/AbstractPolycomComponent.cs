using System;

namespace ICD.Connect.Conferencing.Polycom.Devices.Components
{
	public abstract class AbstractPolycomComponent : IDisposable
	{
		/// <summary>
		/// Deconstructor.
		/// </summary>
		~AbstractPolycomComponent()
		{
			Dispose(false);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose(bool disposing)
		{
		}
	}
}
