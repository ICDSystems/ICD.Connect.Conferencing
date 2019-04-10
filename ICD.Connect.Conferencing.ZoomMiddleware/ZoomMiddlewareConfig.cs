namespace ICD.Connect.Conferencing.ZoomMiddleware
{
	public sealed class ZoomMiddlewareConfig
	{
		public string ZoomUsername { get; set; } = "zoom";
		public string ZoomPassword { get; set; } = "zoomus123";
		public ushort ZoomPort { get; set; } = 2244;
		public string ListenAddress { get; set; } = "0.0.0.0";
		public ushort ListenPort { get; set; } = 2245;
	}
}
