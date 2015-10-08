using System;
using System.Collections;
using System.Configuration.Install;
using Microsoft.Win32;
using Shuttle.Core.Infrastructure;

namespace Shuttle.Core.Host
{
	public class WindowsServiceInstaller
	{
		public void Install(InstallConfiguration installerConfiguration)
		{
			Guard.AgainstNull(installerConfiguration, "configuration");

			installerConfiguration.ApplyInvariants();

			ColoredConsole.WriteLine(ConsoleColor.Green, "Installing service '{0}'.", installerConfiguration.InstancedServiceName());

			using (var installer = new AssemblyInstaller(installerConfiguration.ServiceAssembly ?? typeof (Host).Assembly, null))
			{
				IDictionary state = new Hashtable();

				installer.UseNewContext = true;

				try
				{
					installer.Install(state);
					installer.Commit(state);

					var serviceKey = GetServiceKey(installerConfiguration.InstancedServiceName());

					serviceKey.SetValue("Description", installerConfiguration.Description);
					serviceKey.SetValue("ImagePath",
						string.Format("{0} /serviceName:\"{1}\"{2}{3}{4}",
							serviceKey.GetValue("ImagePath"),
							installerConfiguration.ServiceName,
							string.IsNullOrEmpty(installerConfiguration.Instance)
								? string.Empty
								: string.Format(" /instance:\"{0}\"", installerConfiguration.Instance),
							string.IsNullOrEmpty(installerConfiguration.ConfigurationFileName)
								? string.Empty
								: string.Format(" /configurationFileName:\"{0}\"", installerConfiguration.ConfigurationFileName),
							string.Format(" /hostType:\"{0}\"", installerConfiguration.HostTypeAssemblyQualifiedName)));
				}
				catch
				{
					installer.Rollback(state);

					throw;
				}

				ColoredConsole.WriteLine(ConsoleColor.Green, "Service '{0}' has been successfully installed.", installerConfiguration.InstancedServiceName());
			}
		}

		public void Uninstall(ServiceInstallerConfiguration installerConfiguration)
		{
			Guard.AgainstNull(installerConfiguration, "configuration");

			installerConfiguration.ApplyInvariants();

			ColoredConsole.WriteLine(ConsoleColor.Green, "Uninstalling service '{0}'.", installerConfiguration.InstancedServiceName());

			using (var installer = new AssemblyInstaller(installerConfiguration.ServiceAssembly ?? typeof(Host).Assembly, null))
			{
				IDictionary state = new Hashtable();

				installer.UseNewContext = true;

				try
				{
					installer.Uninstall(state);
				}
				catch
				{
					installer.Rollback(state);

					throw;
				}
			}
		}

		public RegistryKey GetServiceKey(string serviceName)
		{
			var system = Registry.LocalMachine.OpenSubKey("System");

			if (system != null)
			{
				var currentControlSet = system.OpenSubKey("CurrentControlSet");

				if (currentControlSet != null)
				{
					var services = currentControlSet.OpenSubKey("Services");

					if (services != null)
					{
						var service = services.OpenSubKey(serviceName, true);

						if (service != null)
						{
							return service;
						}
					}
				}
			}

			throw new Exception(string.Format("Could not get registry key for service '{0}'.", serviceName));
		}
	}
}