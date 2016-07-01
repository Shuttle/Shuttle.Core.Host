using System;
using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.Runtime.Remoting.Messaging;
using System.ServiceProcess;
using Shuttle.Core.Infrastructure;

namespace Shuttle.Core.Host
{
    [RunInstaller(true)]
    public class HostServiceInstaller : Installer
    {
        private ILog _log;

        public HostServiceInstaller()
        {
            _log = Log.For(this);
        }

        public override void Install(IDictionary stateSaver)
        {
            var configuration =
                (InstallConfiguration)CallContext.LogicalGetData(WindowsServiceInstaller.InstallConfigurationKey);

            if (configuration == null)
            {
                throw new InstallException("No install configuration could be located in the call context.");
            }

            var hasUserName = !string.IsNullOrEmpty(configuration.UserName);
            var hasPassword = !string.IsNullOrEmpty(configuration.Password);

            if (hasUserName && !hasPassword)
            {
                throw new InstallException("A username has been specified without a password.  Please specify both or none.");
            }

            if (hasPassword && !hasUserName)
            {
                throw new InstallException("A password has been specified without a username.  Please specify both or none.");
            }

            var processInstaller = new ServiceProcessInstaller();

            if (hasUserName && hasPassword)
            {
                _log.Trace(string.Format("[ServiceAccount] : username = '{0}' with specified password", configuration.UserName));

                processInstaller.Account = ServiceAccount.User;
                processInstaller.Username = configuration.UserName;
                processInstaller.Password = configuration.Password;
            }
            else
            {
                _log.Trace("[ServiceAccount] : LocalSystem");

                processInstaller.Account = ServiceAccount.LocalSystem;
            }

            var installer = new ServiceInstaller
            {
                DisplayName = configuration.DisplayName,
                ServiceName = configuration.InstancedServiceName(),
                Description = configuration.Description,
                StartType = configuration.StartManually
                    ? ServiceStartMode.Manual
                    : ServiceStartMode.Automatic
            };

            Installers.Add(processInstaller);
            Installers.Add(installer);

            base.Install(stateSaver);
        }

        public override void Uninstall(IDictionary savedState)
        {
            var configuration =
                (ServiceInstallerConfiguration)
                    CallContext.LogicalGetData(WindowsServiceInstaller.ServiceInstallerConfigurationKey);

            if (configuration == null)
            {
                throw new InstallException("No uninstall configuration could be located in the call context.");
            }

            var processInstaller = new ServiceProcessInstaller();

            var installer = new ServiceInstaller
            {
                ServiceName = configuration.InstancedServiceName()
            };

            Installers.Add(processInstaller);
            Installers.Add(installer);

            base.Uninstall(savedState);
        }
    }
}