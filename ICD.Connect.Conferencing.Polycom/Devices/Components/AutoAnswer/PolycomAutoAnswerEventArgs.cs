using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Conferencing.Polycom.Devices.Components.AutoAnswer
{
	public enum eAutoAnswer
	{
		No,
		Yes,
		DoNotDisturb
	}

	public sealed class PolycomAutoAnswerEventArgs : GenericEventArgs<eAutoAnswer>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public PolycomAutoAnswerEventArgs(eAutoAnswer data)
			: base(data)
		{
		}
	}
}
