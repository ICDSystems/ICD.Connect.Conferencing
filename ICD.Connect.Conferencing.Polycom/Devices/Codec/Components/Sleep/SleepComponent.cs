using System;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Sleep
{
	public sealed class SleepComponent : AbstractPolycomComponent, IFeedBackComponent
    {
		/// <summary>
		/// Raised when the awake state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnAwakeStateChanged;

		private bool m_Awake;

		/// <summary>
		/// Gets the awake state.
		/// </summary>
		public bool Awake
		{
			get { return m_Awake; }
			private set
			{
				if (value == m_Awake)
					return;

				m_Awake = value;

				Codec.Logger.Set("Awake", eSeverity.Informational, m_Awake);

				OnAwakeStateChanged.Raise(this, new BoolEventArgs(m_Awake));
			}
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="codec"></param>
		public SleepComponent(PolycomGroupSeriesDevice codec)
			: base(codec)
		{
			Subscribe(Codec);

			Codec.RegisterFeedback("listen", HandleListen);

			if (Codec.Initialized)
				Initialize();
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			OnAwakeStateChanged = null;

			base.Dispose(disposing);
		}

		/// <summary>
		/// Called to initialize the component.
		/// </summary>
		protected override void Initialize()
		{
			base.Initialize();

			InitializeFeedBack();
		}

		/// <summary>
		/// Called to initialize the feedbacks.
		/// </summary>
		public void InitializeFeedBack()
		{
			Codec.EnqueueCommand("sleep register");
		}

		/// <summary>
		/// Handles listen messages from the device.
		/// </summary>
		/// <param name="data"></param>
		private void HandleListen(string data)
		{
			switch (data)
			{
				case "listen going to sleep":
					Awake = false;
					break;

				case "listen waking up":
					Awake = true;
					break;
			}
		}

		#region Methods

		/// <summary>
		/// Puts the device to sleep.
		/// </summary>
		public void Sleep()
		{
			Codec.EnqueueCommand("sleep");
			Codec.Logger.Log(eSeverity.Informational, "Putting device to sleep");
		}

		/// <summary>
		/// Wakes the device.
		/// </summary>
		public void Wake()
		{
			Codec.EnqueueCommand("wake");
			Codec.Logger.Log(eSeverity.Informational, "Waking device");
		}

		/// <summary>
		/// Sets the sleep mute state.
		/// </summary>
		/// <param name="enabled"></param>
		public void SetSleepMute(bool enabled)
		{
			Codec.EnqueueCommand("sleep mute {0}", enabled ? "on" : "off");
			Codec.Logger.Log(eSeverity.Informational, "Setting sleep mute {0}", enabled ? "on" : "off");
		}

		#endregion
	}
}
