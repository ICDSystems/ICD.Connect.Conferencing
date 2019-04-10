#if !SIMPLSHARP
using System;
using System.Linq;
using ICD.Common.Utils.IO;
using Topshelf;
using Topshelf.StartParameters;

namespace ICD.Connect.Conferencing.ZoomMiddleware
{
	public static class Program
	{
		/// <summary>
		/// Run as service.
		/// </summary>
		public static void Main()
		{
			ZoomMiddlewareConfig config = new ZoomMiddlewareConfig();

			TopshelfExitCode rc = HostFactory.Run(x =>
			{
				x.EnableStartParameters();

				x.WithStartParameter("zoomUsername", u => config.ZoomUsername = u);
				x.WithStartParameter("zoomPassword", p => config.ZoomPassword = p);
				x.WithStartParameter("zoomPort", p => config.ZoomPort = ushort.Parse(p));
				x.WithStartParameter("listenAddress", a => config.ListenAddress = a);
				x.WithStartParameter("listenPort", p => config.ListenPort = ushort.Parse(p));

				x.Service<ZoomMiddleware>(s =>
				{
					s.ConstructUsing(n => Construct(config));
					s.WhenStarted(Start);
					s.WhenStopped(Stop);
				});

				x.RunAsLocalSystem();

				x.SetDisplayName("ICD Zoom Middleware Service");
				x.SetServiceName("ICD Zoom Middleware Service");
				x.SetDescription("Load balancing for Zoom conferencing");
				
				x.SetStartTimeout(TimeSpan.FromMinutes(10));
				x.SetStopTimeout(TimeSpan.FromMinutes(10));

				x.AfterUninstall(AfterUninstall);

			    x.StartAutomatically();
			});

			int exitCode = (int)Convert.ChangeType(rc, rc.GetTypeCode());
			Environment.ExitCode = exitCode;
		}

		private static ZoomMiddleware Construct(ZoomMiddlewareConfig config)
		{
			return new ZoomMiddleware(config);
		}

		private static void Start(ZoomMiddleware middleware)
		{
			middleware.Start();
		}

		private static void Stop(ZoomMiddleware middleware)
		{
			middleware.Stop();
		}

		private static void AfterUninstall()
		{
			// Remove the Zoom Middleware program data
			IcdDirectory.Delete(ZoomMiddleware.ZoomMiddlewarePath, true);

			// If the ICD Systems directory is empty, remove it
			if (!IcdDirectory.GetFiles(ZoomMiddleware.IcdSystemsPath).Any() &&
				!IcdDirectory.GetDirectories(ZoomMiddleware.IcdSystemsPath).Any())
				IcdDirectory.Delete(ZoomMiddleware.IcdSystemsPath, true);
		}
	}
}
#endif
