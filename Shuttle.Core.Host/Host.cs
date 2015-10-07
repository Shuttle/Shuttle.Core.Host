using System;
using System.Collections;
using System.Configuration;
using System.Configuration.Install;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using Microsoft.Win32;
using Shuttle.Core.Infrastructure;

namespace Shuttle.Core.Host
{
	public class Host
	{
		private bool runServiceException;

		public void RunService(IHost host, IHostServiceConfiguration hostServiceConfiguration)
		{
			Guard.AgainstNull(host, "host");
			Guard.AgainstNull(hostServiceConfiguration, "hostServiceConfiguration");

			try
			{
				var configurationFile = GetHostConfigurationFile(host, hostServiceConfiguration);

				if (!File.Exists(configurationFile))
				{
					throw new ApplicationException(string.Format("Cannot find host configuration file '{0}'", configurationFile));
				}

				AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", configurationFile);

				var state = typeof (ConfigurationManager).GetField("s_initState", BindingFlags.NonPublic | BindingFlags.Static);

				if (state == null)
				{
					throw new ApplicationException(string.Format("Could not obtain 's_initState' from ConfigurationManager."));
				}

				state.SetValue(null, 0);

				var system = typeof (ConfigurationManager).GetField("s_configSystem", BindingFlags.NonPublic | BindingFlags.Static);

				if (system == null)
				{
					throw new ApplicationException(string.Format("Could not obtain 's_configSystem' from ConfigurationManager."));
				}

				system.SetValue(null, null);

				var current = typeof (ConfigurationManager)
					.Assembly.GetTypes().First(x => x.FullName == "System.Configuration.ClientConfigPaths")
					.GetField("s_current", BindingFlags.NonPublic | BindingFlags.Static);

				if (current == null)
				{
					throw new ApplicationException(
						string.Format("Could not obtain 's_current' from System.Configuration.ClientConfigPaths."));
				}

				current.SetValue(null, null);

				if (!Environment.UserInteractive)
				{
					ServiceBase.Run(new ServiceBase[]
					{
						new HostService(host, hostServiceConfiguration)
					});
				}
				else
				{
					Console.CursorVisible = false;

					ConsoleService(host, hostServiceConfiguration);
				}
			}
			catch (Exception ex)
			{
				if (Environment.UserInteractive)
				{
					runServiceException = true;

					ColoredConsole.WriteLine(ConsoleColor.Red, ex.AllMessages());
					ColoredConsole.WriteLine(ConsoleColor.Red, ex.StackTrace);

					Console.WriteLine();
					ColoredConsole.WriteLine(ConsoleColor.DarkYellow, "[press any key to close]");
					Console.ReadKey();
				}
				else
				{
					throw;
				}
			}
		}

		private void ConsoleService(IHost host, IHostServiceConfiguration hostServiceConfiguration)
		{
			Guard.AgainstNull(host, "host");
			Guard.AgainstNull(hostServiceConfiguration, "hostServiceConfiguration");

			if (ServiceController.GetServices().Any(s => s.ServiceName == hostServiceConfiguration.ServiceName))
			{
				ColoredConsole.WriteLine(ConsoleColor.Yellow,
					"WARNING: Windows service '{0}' is running.  The display name is '{1}'.",
					hostServiceConfiguration.ServiceName, hostServiceConfiguration.DisplayName);
				Console.WriteLine();
			}

			var waitHandle = new ManualResetEvent(false);
			var waitHandles = new WaitHandle[] {waitHandle};

			Console.CancelKeyPress += ((sender, e) =>
			{
				if (!runServiceException)
				{
					ColoredConsole.WriteLine(ConsoleColor.Green, "[stopping]");
				}
				else
				{
					ColoredConsole.WriteLine(ConsoleColor.DarkYellow,
						"[press any key to close (ctrl+c does not work)]");
				}

				waitHandle.Set();

				e.Cancel = true;
			});

			host.Start();

			Console.WriteLine();
			ColoredConsole.WriteLine(ConsoleColor.Green, "Shuttle.Core.Host started for '{0}'.",
				hostServiceConfiguration.ServiceName);
			Console.WriteLine();
			ColoredConsole.WriteLine(ConsoleColor.DarkYellow, "[press ctrl+c to stop]");
			Console.WriteLine();

			WaitHandle.WaitAny(waitHandles);

			var disposable = host as IDisposable;

			if (disposable != null)
			{
				disposable.Dispose();
			}
		}

		private static string GetHostConfigurationFile(IHost host, IHostServiceConfiguration hostServiceConfiguration)
		{
			return Path.Combine(
				AppDomain.CurrentDomain.BaseDirectory,
				string.IsNullOrEmpty(hostServiceConfiguration.ConfigurationFileName)
					? string.Concat(host.GetType().Assembly.ManifestModule.Name, ".config")
					: hostServiceConfiguration.ConfigurationFileName);
		}

		public static void Install(IHostServiceConfiguration hostServiceConfiguration)
		{
			ColoredConsole.WriteLine(ConsoleColor.Green, "Installing service '{0}'.", hostServiceConfiguration.ServiceName);

			using (var installer = new AssemblyInstaller(typeof (Host).Assembly, null))
			{
				IDictionary state = new Hashtable();

				installer.UseNewContext = true;

				try
				{
					installer.Install(state);
					installer.Commit(state);

					var ok = true;

					var system = Registry.LocalMachine.OpenSubKey("System");

					if (system != null)
					{
						var currentControlSet = system.OpenSubKey("CurrentControlSet");

						if (currentControlSet != null)
						{
							var services = currentControlSet.OpenSubKey("Services");

							if (services != null)
							{
								var service = services.OpenSubKey(hostServiceConfiguration.ServiceName, true);

								if (service != null)
								{
									service.SetValue("Description", hostServiceConfiguration.Description);
									service.SetValue("ImagePath",
										string.Format("{0} /serviceName:\"{1}\"{2}{3}{4}",
											service.GetValue("ImagePath"),
											hostServiceConfiguration.ServiceName,
											string.IsNullOrEmpty(hostServiceConfiguration.Instance)
												? string.Empty
												: string.Format(" /instance:\"{0}\"", hostServiceConfiguration.Instance),
											string.IsNullOrEmpty(hostServiceConfiguration.ConfigurationFileName)
												? string.Empty
												: string.Format(" /configurationFileName:\"{0}\"", hostServiceConfiguration.ConfigurationFileName),
											string.Format(" /hostType:\"{0}\"", hostServiceConfiguration.HostTypeAssemblyQualifiedName())));
								}
								else
								{
									ok = false;
								}
							}
							else
							{
								ok = false;
							}
						}
						else
						{
							ok = false;
						}
					}
					else
					{
						ok = false;
					}

					if (!ok)
					{
						throw new ConfigurationErrorsException("Could not set registry values for the service.");
					}
				}
				catch
				{
					installer.Rollback(state);

					throw;
				}
			}
		}

		public static void Uninstall(IHostServiceConfiguration hostServiceConfiguration)
		{
			ColoredConsole.WriteLine(ConsoleColor.Green, "Uninstalling service '{0}'.", hostServiceConfiguration.ServiceName);

			using (var installer = new AssemblyInstaller(typeof (Host).Assembly, null))
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
	}
}