using System;
using System.Diagnostics;
using System.ServiceProcess;
using Shuttle.Core.Infrastructure;

namespace Shuttle.Core.Host
{
    public sealed class HostService : ServiceBase
    {
        private readonly HostServiceConfiguration _hostServiceConfiguration;
        private readonly HostEventLog _log;

        public HostService(HostServiceConfiguration hostServiceConfiguration)
        {
            Guard.AgainstNull(hostServiceConfiguration, "hostServiceConfiguration");

            _hostServiceConfiguration = hostServiceConfiguration;

            ServiceName = hostServiceConfiguration.ServiceName;

            _log = new HostEventLog(ServiceName);

            AppDomain.CurrentDomain.UnhandledException += UnhandledException;
        }

        private void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = (Exception)e.ExceptionObject;

            _log.WrinteEntry(ex.Message, EventLogEntryType.Error);
        }

        protected override void OnStart(string[] args)
        {
            _log.WrinteEntry(string.Format("[starting] : service name = '{0}'", ServiceName));

            _hostServiceConfiguration.Host.Start();

            _log.WrinteEntry(string.Format("[started] : service name = '{0}'", ServiceName));
        }

        protected override void OnStop()
        {
            _log.WrinteEntry(string.Format("[stopping] : service name = '{0}'", ServiceName));

            var disposable = _hostServiceConfiguration.Host as IDisposable;

            if (disposable != null)
            {
                disposable.Dispose();
            }

            _log.WrinteEntry(string.Format("[stopped] : service name = '{0}'", ServiceName));
        }
    }
}