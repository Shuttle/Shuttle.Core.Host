using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace Shuttle.Core.Host
{
    [RunInstaller(true)]
    public class HostServiceInstaller : Installer
    {
	    private static IHostServiceConfiguration _hostServiceConfiguration;

        public HostServiceInstaller()
        {
	        if (_hostServiceConfiguration == null)
	        {
				throw new InstallException("No host service configuration has been set.  Cannot install.");
	        }

	        var processInstaller = new ServiceProcessInstaller();

			if (!string.IsNullOrEmpty(_hostServiceConfiguration.UserName)
                &&
                !string.IsNullOrEmpty(_hostServiceConfiguration.Password))
            {
                processInstaller.Account = ServiceAccount.User;
                processInstaller.Username = _hostServiceConfiguration.UserName;
                processInstaller.Password = _hostServiceConfiguration.Password;
            }
            else
            {
                processInstaller.Account = ServiceAccount.LocalSystem;
            }

            var installer = new ServiceInstaller
                            {
                                DisplayName = _hostServiceConfiguration.DisplayName,
                                ServiceName = _hostServiceConfiguration.ServiceName,
                                Description = _hostServiceConfiguration.Description,
                                StartType = _hostServiceConfiguration.StartManually
                                                ? ServiceStartMode.Manual
                                                : ServiceStartMode.Automatic
                            };

            Installers.Add(processInstaller);
            Installers.Add(installer);
        }

	    public static void Assign(IHostServiceConfiguration hostServiceConfiguration)
	    {
		    _hostServiceConfiguration = hostServiceConfiguration;
	    }
    }
}