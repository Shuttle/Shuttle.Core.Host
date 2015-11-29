using System;
using System.Collections;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Runtime.Remoting.Messaging;
using System.Security;
using System.Security.Principal;
using Microsoft.Win32;
using Shuttle.Core.Infrastructure;

namespace Shuttle.Core.Host
{
    public class WindowsServiceInstaller : MarshalByRefObject
    {
        public static readonly string InstallConfigurationKey = "__InstallConfigurationKey__";
        public static readonly string ServiceInstallerConfigurationKey = "__ServiceInstallerConfigurationKey__";

        public void Install(params string[] args)
        {
            Install(new Arguments(args ?? new string[] { }));
        }

        public void Install(Arguments arguments)
        {
            Guard.AgainstNull(arguments, "arguments");

            Install(new InstallConfiguration(arguments));
        }

        public void Install(InstallConfiguration installConfiguration)
        {
            GuardAdministrator();

            Guard.AgainstNull(installConfiguration, "installConfiguration");

            if (!string.IsNullOrEmpty(installConfiguration.ServiceAssemblyPath))
            {
                InstallRemoteService(installConfiguration);

                return;
            }

            if (installConfiguration.HostTypeRequired)
            {
                installConfiguration.ApplyHostType(
                    new HostFactory().GetHostType(installConfiguration.HostTypeAssemblyQualifiedName));
            }

            var log = HostEventLog.GetEventLog(installConfiguration.InstancedServiceName());

            CallContext.LogicalSetData(InstallConfigurationKey, installConfiguration);

            installConfiguration.ApplyInvariants();

            ColoredConsole.WriteLine(ConsoleColor.Green, "Installing service '{0}'.",
                installConfiguration.InstancedServiceName());

            var assemblyInstaller = new AssemblyInstaller(typeof(Host).Assembly, null);

            using (var installer = assemblyInstaller)
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
                                : string.Format(" /configurationFileName:\"{0}\"",
                                    installConfiguration.ConfigurationFileName),
                            string.Format(" /hostType:\"{0}\"", installConfiguration.HostTypeAssemblyQualifiedName)));
                }
                catch (Exception ex)
                {
                    try
                    {
                        installer.Rollback(state);
                    }
                    catch (InstallException installException)
                    {
                        ColoredConsole.WriteLine(ConsoleColor.DarkYellow, installException.Message);
                    }

                    log.WriteEntry(ex.Message, EventLogEntryType.Error);

                    throw;
                }

                var message = string.Format("Service '{0}' has been successfully installed.", installConfiguration.InstancedServiceName());

                log.WriteEntry(message);

                ColoredConsole.WriteLine(ConsoleColor.Green, message);
            }
        }

        private void GuardAdministrator()
        {
            var windowsIdentity = WindowsIdentity.GetCurrent();

            if (windowsIdentity == null)
            {
                throw new SecurityException(
                    "Could not get the current Windows identity.  Cannot determine if the identity is an administrator.");
            }

            var securityIdentifier = windowsIdentity.Owner;

            if (securityIdentifier == null)
            {
                throw new SecurityException(
                    "Could not get the current Windows identity's security identifier.  Cannot determine if the identity is an administrator.");
            }

            if (securityIdentifier.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid))
            {
                return;
            }

            throw new SecurityException(
                string.Format(
                    "Windows identity '{0}' is not an administrator.  Administrator privilege is required to install or uninstall a service.",
                    windowsIdentity.Name));
        }

        private void InstallRemoteService(InstallConfiguration installConfiguration)
        {
            var domain = RemoteDomain(installConfiguration.ServiceAssemblyPath);

            try
            {
                var installer =
                    (WindowsServiceInstaller)
                        domain.CreateInstanceFromAndUnwrap(installConfiguration.ServiceAssemblyPath,
                            typeof(WindowsServiceInstaller).FullName);
                var configuration =
                    (InstallConfiguration)
                        domain.CreateInstanceFromAndUnwrap(installConfiguration.ServiceAssemblyPath,
                            typeof(InstallConfiguration).FullName);

                configuration.ConfigurationFileName = installConfiguration.ConfigurationFileName;
                configuration.Description = installConfiguration.Description;
                configuration.DisplayName = installConfiguration.DisplayName;
                configuration.StartManually = installConfiguration.StartManually;
                configuration.UserName = installConfiguration.UserName;
                configuration.Password = installConfiguration.Password;
                configuration.HostTypeAssemblyQualifiedName = installConfiguration.HostTypeAssemblyQualifiedName;
                configuration.Instance = installConfiguration.Instance;
                configuration.ServiceName = installConfiguration.ServiceName;

                installer.Install(configuration);
            }
            finally
            {
                AppDomain.Unload(domain);
            }
        }

        private static AppDomain RemoteDomain(string serviceAssemblyPath)
        {
            if (!File.Exists(serviceAssemblyPath))
            {
                throw new ApplicationException(string.Format("Service assembly path '{0}' does not exist.",
                    serviceAssemblyPath));
            }

            var setup = new AppDomainSetup
            {
                ApplicationBase = Path.GetDirectoryName(serviceAssemblyPath)
            };

            return AppDomain.CreateDomain("installer", AppDomain.CurrentDomain.Evidence, setup);
        }

        private void UninstallRemoteService(ServiceInstallerConfiguration serviceInstallerConfiguration)
        {
            var domain = RemoteDomain(serviceInstallerConfiguration.ServiceAssemblyPath);

            try
            {
                var installer =
                    (WindowsServiceInstaller)
                        domain.CreateInstanceFromAndUnwrap(serviceInstallerConfiguration.ServiceAssemblyPath,
                            typeof(WindowsServiceInstaller).FullName);
                var configuration =
                    (ServiceInstallerConfiguration)
                        domain.CreateInstanceFromAndUnwrap(serviceInstallerConfiguration.ServiceAssemblyPath,
                            typeof(ServiceInstallerConfiguration).FullName);

                configuration.HostTypeAssemblyQualifiedName =
                    serviceInstallerConfiguration.HostTypeAssemblyQualifiedName;
                configuration.Instance = serviceInstallerConfiguration.Instance;
                configuration.ServiceName = serviceInstallerConfiguration.ServiceName;

                installer.Uninstall(configuration);
            }
            finally
            {
                AppDomain.Unload(domain);
            }
        }

        public void Uninstall(params string[] args)
        {
            Uninstall(new Arguments(args ?? new string[] { }));
        }

        public void Uninstall(Arguments arguments)
        {
            Guard.AgainstNull(arguments, "arguments");

            Uninstall(new ServiceInstallerConfiguration(arguments));
        }

        public void Uninstall(ServiceInstallerConfiguration serviceConfiguration)
        {
            GuardAdministrator();

            Guard.AgainstNull(serviceConfiguration, "serviceConfiguration");

            if (!string.IsNullOrEmpty(serviceConfiguration.ServiceAssemblyPath))
            {
                UninstallRemoteService(serviceConfiguration);

                return;
            }

            if (serviceConfiguration.HostTypeRequired)
            {
                serviceConfiguration.ApplyHostType(
                    new HostFactory().GetHostType(serviceConfiguration.HostTypeAssemblyQualifiedName));
            }

            var log = HostEventLog.GetEventLog(serviceConfiguration.InstancedServiceName());

            CallContext.LogicalSetData(ServiceInstallerConfigurationKey, serviceConfiguration);

            serviceConfiguration.ApplyInvariants();

            ColoredConsole.WriteLine(ConsoleColor.Green, "Uninstalling service '{0}'.",
                serviceConfiguration.InstancedServiceName());

            using (var installer = new AssemblyInstaller(typeof(Host).Assembly, null))
            {
                IDictionary state = new Hashtable();

                installer.UseNewContext = true;

                try
                {
                    installer.Uninstall(state);
                }
                catch (Exception ex)
                {
                    try
                    {
                        installer.Rollback(state);
                    }
                    catch (InstallException installException)
                    {
                        ColoredConsole.WriteLine(ConsoleColor.DarkYellow, installException.Message);
                    }

                    log.WriteEntry(ex.Message, EventLogEntryType.Error);

                    throw;
                }
            }

            var message = string.Format("Service '{0}' has been successfully uninstalled.", serviceConfiguration.InstancedServiceName());

            log.WriteEntry(message);

            ColoredConsole.WriteLine(ConsoleColor.Green, message);
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