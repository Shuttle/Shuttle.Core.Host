using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace Shuttle.Core.Host
{
    [RunInstaller(true)]
    public class HostServiceInstaller : Installer
    {
        public HostServiceInstaller()
        {
            var processInstaller = new ServiceProcessInstaller();

            if (!string.IsNullOrEmpty(Host.ServiceConfiguration.UserName)
                &&
                !string.IsNullOrEmpty(Host.ServiceConfiguration.Password))
            {
                processInstaller.Account = ServiceAccount.User;
                processInstaller.Username = Host.ServiceConfiguration.UserName;
                processInstaller.Password = Host.ServiceConfiguration.Password;
            }
            else
            {
                processInstaller.Account = ServiceAccount.LocalSystem;
            }

            var installer = new ServiceInstaller
                            {
                                DisplayName = Host.ServiceConfiguration.DisplayName,
                                ServiceName = Host.ServiceConfiguration.ServiceName,
                                Description = Host.ServiceConfiguration.Description,
                                StartType = Host.ServiceConfiguration.StartManually
                                                ? ServiceStartMode.Manual
                                                : ServiceStartMode.Automatic
                            };

            Installers.Add(processInstaller);
            Installers.Add(installer);
        }
    }
}