using System;

namespace ICD.Connect.Conferencing.Devices
{
	[Flags]
    public enum eCodecInputType
    {
		None = 0,
		Content = 1,
		Camera = 2,
		Integrated = 4
    }
}
