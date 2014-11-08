using System;
using System.ServiceProcess;
using Shuttle.Core.Infrastructure;

namespace Shuttle.Core.Host
{
    public class HostService : ServiceBase
    {
        private readonly IHostServiceConfiguration configuration;

        public HostService(IHostServiceConfiguration configuration)
        {
            this.configuration = configuration;

            ServiceName = configuration.ServiceName;
        }

        protected override void OnStart(string[] args)
        {
            configuration.Host.Start();

            Log.For(this).Information(string.Format("'{0}' service has started.", configuration.DisplayName));
        }

        protected override void OnStop()
        {
            var disposable = configuration.Host as IDisposable;

            if (disposable != null)
            {
                disposable.Dispose();
            }

            Log.For(this).Information(string.Format("'{0}' service has stopped.", configuration.DisplayName));
        }
    }
}