using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Services.Logging;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Button
{
	public sealed class ButtonComponent : AbstractPolycomComponent
	{
		public enum eDigit 
		{
			Pound,
			Star,
			Zero,
			One,
			Two,
			Three,
			Four,
			Five,
			Six,
			Seven,
			Eight,
			Nine,
			Period
		}

		public enum eDPad
		{
			Up,
			Down,
			Left,
			Right,
			Select
		}

		public enum eCall
		{
			Back,
			Call,
			Graphics,
			Hangup
		}

		public enum eVolume
		{
			Mute,
			VolumeUp,
			VolumeDown
		}

		public enum eMisc
		{
			Camera,
			Delete,
			Directory,
			Home,
			Keyboard,
			Menu,
			Period,
			Pip,
			Preset,
			Info
		}

		private static readonly BiDictionary<eDigit, string> s_DigitNames =
			new BiDictionary<eDigit, string>
			{
				{eDigit.Pound, "#"},
				{eDigit.Star, "*"},
				{eDigit.Zero, "0"},
				{eDigit.One, "1"},
				{eDigit.Two, "2"},
				{eDigit.Three, "3"},
				{eDigit.Four, "4"},
				{eDigit.Five, "5"},
				{eDigit.Six, "6"},
				{eDigit.Seven, "7"},
				{eDigit.Eight, "8"},
				{eDigit.Nine, "9"},
				{eDigit.Period, "."}
			};

		private static readonly BiDictionary<eDPad, string> s_DPadNames =
			new BiDictionary<eDPad, string>
			{
				{eDPad.Up, "up"},
				{eDPad.Down, "down"},
				{eDPad.Left, "left"},
				{eDPad.Right, "right"},
				{eDPad.Select, "select"}
			};

		private static readonly BiDictionary<eCall, string> s_CallNames =
			new BiDictionary<eCall, string>
			{
				{eCall.Back, "back"},
				{eCall.Call, "call"},
				{eCall.Graphics, "graphics"},
				{eCall.Hangup, "hangup"},
			};

		private static readonly BiDictionary<eVolume, string> s_VolumeNames =
			new BiDictionary<eVolume, string>
			{
				{eVolume.Mute, "mute"},
				{eVolume.VolumeUp, "volume+"},
				{eVolume.VolumeDown, "volume-"},
			};

		private static readonly BiDictionary<eMisc, string> s_MiscNames =
			new BiDictionary<eMisc, string>
			{
				{eMisc.Camera, "camera"},
				{eMisc.Delete, "delete"},
				{eMisc.Directory, "directory"},
				{eMisc.Home, "home"},
				{eMisc.Keyboard, "keyboard"},
				{eMisc.Menu, "menu"},
				{eMisc.Period, "period"},
				{eMisc.Pip, "pip"},
				{eMisc.Preset, "preset"},
				{eMisc.Info, "info"},
			};

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="codec"></param>
		public ButtonComponent(PolycomGroupSeriesDevice codec)
			: base(codec)
		{
		}

		/// <summary>
		/// Presses the given button.
		/// </summary>
		/// <param name="button"></param>
		public void PressButton(eDigit button)
		{
			string name = s_DigitNames.GetValue(button);

			Codec.Log(eSeverity.Informational, "Pressing button {0}", name);
			Codec.SendCommand("button " + name);
		}

		/// <summary>
		/// Presses the given button.
		/// </summary>
		/// <param name="button"></param>
		public void PressButton(eDPad button)
		{
			string name = s_DPadNames.GetValue(button);

			Codec.Log(eSeverity.Informational, "Pressing button {0}", name);
			Codec.SendCommand("button " + name);
		}

		/// <summary>
		/// Presses the given button.
		/// </summary>
		/// <param name="button"></param>
		public void PressButton(eCall button)
		{
			string name = s_CallNames.GetValue(button);

			Codec.Log(eSeverity.Informational, "Pressing button {0}", name);
			Codec.SendCommand("button " + name);
		}

		/// <summary>
		/// Presses the given button.
		/// </summary>
		/// <param name="button"></param>
		public void PressButton(eVolume button)
		{
			string name = s_VolumeNames.GetValue(button);

			Codec.Log(eSeverity.Informational, "Pressing button {0}", name);
			Codec.SendCommand("button " + name);
		}

		/// <summary>
		/// Presses the given button.
		/// </summary>
		/// <param name="button"></param>
		public void PressButton(eMisc button)
		{
			string name = s_MiscNames.GetValue(button);

			Codec.Log(eSeverity.Informational, "Pressing button {0}", name);
			Codec.SendCommand("button " + name);
		}
	}
}
