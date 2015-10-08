using System;
using System.ServiceProcess;
using Shuttle.Core.Infrastructure;

namespace Shuttle.Core.Host
{
	public class HostService : ServiceBase
	{
		private readonly HostServiceConfiguration _hostServiceConfiguration;

		public HostService(HostServiceConfiguration hostServiceConfiguration)
		{
			Guard.AgainstNull(hostServiceConfiguration, "hostServiceConfiguration");

			_hostServiceConfiguration = hostServiceConfiguration;

			ServiceName = hostServiceConfiguration.ServiceName;
		}

		protected override void OnStart(string[] args)
		{
			_hostServiceConfiguration.Host.Start();

			Log.For(this).Information(string.Format("'{0}' service has started.", _hostServiceConfiguration.ServiceName));
		}

		protected override void OnStop()
		{
			var disposable = _hostServiceConfiguration.Host as IDisposable;

			if (disposable != null)
			{
				disposable.Dispose();
			}

			Log.For(this).Information(string.Format("'{0}' service has stopped.", _hostServiceConfiguration.ServiceName));
		}
	}
}