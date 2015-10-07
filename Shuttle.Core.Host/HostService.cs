using System;
using System.ServiceProcess;
using Shuttle.Core.Infrastructure;

namespace Shuttle.Core.Host
{
    public class HostService : ServiceBase
    {
	    private readonly IHost _host;
	    private readonly IHostServiceConfiguration _hostServiceConfiguration;

        public HostService(IHost host, IHostServiceConfiguration hostServiceConfiguration)
        {
			Guard.AgainstNull(host, "host");
			Guard.AgainstNull(hostServiceConfiguration, "hostServiceConfiguration");

	        _host = host;
	        _hostServiceConfiguration = hostServiceConfiguration;

            ServiceName = hostServiceConfiguration.ServiceName;
        }

        protected override void OnStart(string[] args)
        {
            _host.Start();

            Log.For(this).Information(string.Format("'{0}' service has started.", _hostServiceConfiguration.DisplayName));
        }

        protected override void OnStop()
        {
            var disposable = _host as IDisposable;

            if (disposable != null)
            {
                disposable.Dispose();
            }

            Log.For(this).Information(string.Format("'{0}' service has stopped.", _hostServiceConfiguration.DisplayName));
        }
    }
}