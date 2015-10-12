using System;
using System.Collections;
using System.Configuration.Install;
using System.Runtime.Remoting.Messaging;
using Microsoft.Win32;
using Shuttle.Core.Infrastructure;

namespace Shuttle.Core.Host
{
	public class WindowsServiceInstaller
	{
		public static readonly string InstallConfigurationKey = "__InstallConfigurationKey__";
		public static readonly string ServiceInstallerConfigurationKey = "__ServiceInstallerConfigurationKey__";

		public void Install(InstallConfiguration installConfiguration)
		{
			Guard.AgainstNull(installConfiguration, "configuration");

			CallContext.LogicalSetData(InstallConfigurationKey, installConfiguration);

			installConfiguration.ApplyInvariants();

			ColoredConsole.WriteLine(ConsoleColor.Green, "Installing service '{0}'.", installConfiguration.InstancedServiceName());

			using (var installer = new AssemblyInstaller(installConfiguration.ServiceAssembly ?? typeof (Host).Assembly, null))
			{
				IDictionary state = new Hashtable();

				installer.UseNewContext = true;

				try
				{
					installer.Install(state);
					installer.Commit(state);

					var serviceKey = GetServiceKey(installConfiguration.InstancedServiceName());

					serviceKey.SetValue("Description", installConfiguration.Description);
					serviceKey.SetValue("ImagePath",
						string.Format("{0} /serviceName:\"{1}\"{2}{3}{4}",
							serviceKey.GetValue("ImagePath"),
							installConfiguration.ServiceName,
							string.IsNullOrEmpty(installConfiguration.Instance)
								? string.Empty
								: string.Format(" /instance:\"{0}\"", installConfiguration.Instance),
							string.IsNullOrEmpty(installConfiguration.ConfigurationFileName)
								? string.Empty
								: string.Format(" /configurationFileName:\"{0}\"", installConfiguration.ConfigurationFileName),
							string.Format(" /hostType:\"{0}\"", installConfiguration.HostTypeAssemblyQualifiedName)));
				}
				catch
				{
					try
					{
						installer.Rollback(state);
					}
					catch (InstallException ex)
					{
						ColoredConsole.WriteLine(ConsoleColor.DarkYellow, ex.Message);
					}

					throw;
				}

				ColoredConsole.WriteLine(ConsoleColor.Green, "Service '{0}' has been successfully installed.", installConfiguration.InstancedServiceName());
			}
		}

		public void Uninstall(ServiceInstallerConfiguration installerConfiguration)
		{
			Guard.AgainstNull(installerConfiguration, "configuration");

			CallContext.LogicalSetData(ServiceInstallerConfigurationKey, installerConfiguration);

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
					try
					{
						installer.Rollback(state);
					}
					catch (InstallException ex)
					{
						ColoredConsole.WriteLine(ConsoleColor.DarkYellow, ex.Message);
					}

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