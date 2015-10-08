using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.ServiceProcess;

namespace Shuttle.Core.Host
{
	[RunInstaller(true)]
	public class HostServiceInstaller : Installer
	{
		private static InstallConfiguration _installConfiguration;
		private static ServiceInstallerConfiguration _serviceInstallerConfiguration;

		public override void Install(IDictionary stateSaver)
		{
			if (_installConfiguration == null)
			{
				throw new InstallException("No install configuration has been set.");
			}

			var processInstaller = new ServiceProcessInstaller();

			if (!string.IsNullOrEmpty(_installConfiguration.UserName)
			    &&
			    !string.IsNullOrEmpty(_installConfiguration.Password))
			{
				processInstaller.Account = ServiceAccount.User;
				processInstaller.Username = _installConfiguration.UserName;
				processInstaller.Password = _installConfiguration.Password;
			}
			else
			{
				processInstaller.Account = ServiceAccount.LocalSystem;
			}

			var installer = new ServiceInstaller
			{
				DisplayName = _installConfiguration.DisplayName,
				ServiceName = _installConfiguration.InstancedServiceName(),
				Description = _installConfiguration.Description,
				StartType = _installConfiguration.StartManually
					? ServiceStartMode.Manual
					: ServiceStartMode.Automatic
			};

			Installers.Add(processInstaller);
			Installers.Add(installer);

			base.Install(stateSaver);
		}

		public override void Uninstall(IDictionary savedState)
		{
			if (_serviceInstallerConfiguration == null)
			{
				throw new InstallException("No uninstall configuration has been set.");
			}

			var processInstaller = new ServiceProcessInstaller();

			var installer = new ServiceInstaller
			{
				ServiceName = _serviceInstallerConfiguration.InstancedServiceName()
			};

			Installers.Add(processInstaller);
			Installers.Add(installer);

			base.Uninstall(savedState);
		}

		public static void Assign(InstallConfiguration installConfiguration)
		{
			_installConfiguration = installConfiguration;
		}

		public static void Assign(ServiceInstallerConfiguration serviceInstallerConfiguration)
		{
			_serviceInstallerConfiguration = serviceInstallerConfiguration;
		}
	}
}