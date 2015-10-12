using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.Runtime.Remoting.Messaging;
using System.ServiceProcess;
using Shuttle.Core.Infrastructure;

namespace Shuttle.Core.Host
{
	[RunInstaller(true)]
	public class HostServiceInstaller : Installer
	{
		public override void Install(IDictionary stateSaver)
		{
			var configuration = (InstallConfiguration)CallContext.LogicalGetData(WindowsServiceInstaller.InstallConfigurationKey);

			if (configuration == null)
			{
				throw new InstallException("No install configuration could be located in the call context.");
			}

			var processInstaller = new ServiceProcessInstaller();

			if (!string.IsNullOrEmpty(configuration.UserName)
			    &&
			    !string.IsNullOrEmpty(configuration.Password))
			{
				processInstaller.Account = ServiceAccount.User;
				processInstaller.Username = configuration.UserName;
				processInstaller.Password = configuration.Password;
			}
			else
			{
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
			var configuration = (ServiceInstallerConfiguration)CallContext.LogicalGetData(WindowsServiceInstaller.ServiceInstallerConfigurationKey);

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