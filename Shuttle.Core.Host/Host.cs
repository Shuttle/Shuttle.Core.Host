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
		protected internal static IHostServiceConfiguration ServiceConfiguration;

		private bool runServiceException;

		public void RunService(IHost host, Arguments arguments)
		{
			Guard.AgainstNull(host, "host");

			try
			{
				ServiceConfiguration = GetHostServiceConfiguration(host, arguments);

				if (ServiceConfiguration == null)
				{
					return;
				}

				var install = arguments.Get("install", string.Empty);
				var uninstall = arguments.Get("uninstall", string.Empty);

				if (string.IsNullOrEmpty(install) && string.IsNullOrEmpty(uninstall))
				{
					var configurationFile = GetHostConfigurationFile(host, ServiceConfiguration);

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
						throw new ApplicationException(string.Format("Could not obtain 's_current' from System.Configuration.ClientConfigPaths."));
					}

					current.SetValue(null, null);
				}

				if (!Environment.UserInteractive)
				{
					ServiceBase.Run(new ServiceBase[]
					{
						new HostService(ServiceConfiguration)
					});
				}
				else
				{
					Console.CursorVisible = false;

					if (string.IsNullOrEmpty(install) && string.IsNullOrEmpty(uninstall))
					{
						ConsoleService();

						return;
					}

					if (!string.IsNullOrEmpty(install) && !string.IsNullOrEmpty(uninstall))
					{
						throw new ConfigurationErrorsException("Cannot specify /install and /uninstall together.");
					}

					if (!string.IsNullOrEmpty(install))
					{
						Install(true, arguments, ServiceConfiguration);
					}

					if (!string.IsNullOrEmpty(uninstall))
					{
						Install(false, arguments, ServiceConfiguration);
					}
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

		private void ConsoleService()
		{
			if (ServiceController.GetServices().Any(s => s.ServiceName == ServiceConfiguration.ServiceName))
			{
				ColoredConsole.WriteLine(ConsoleColor.Yellow,
					"WARNING: Windows service '{0}' is running.  The display name is '{1}'.",
					ServiceConfiguration.ServiceName, ServiceConfiguration.DisplayName);
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

			ServiceConfiguration.Host.Start();

			Console.WriteLine();
			ColoredConsole.WriteLine(ConsoleColor.Green, "Shuttle.Core.Host started for '{0}'.",
				ServiceConfiguration.ServiceName);
			Console.WriteLine();
			ColoredConsole.WriteLine(ConsoleColor.DarkYellow, "[press ctrl+c to stop]");
			Console.WriteLine();

			WaitHandle.WaitAny(waitHandles);

			var disposable = ServiceConfiguration.Host as IDisposable;

			if (disposable != null)
			{
				disposable.Dispose();
			}
		}

		private static string GetHostConfigurationFile(IHost host, IHostServiceConfiguration configuration)
		{
			return Path.Combine(
				AppDomain.CurrentDomain.BaseDirectory,
				string.IsNullOrEmpty(configuration.ConfigurationFileName)
					? string.Concat(host.GetType().Assembly.ManifestModule.Name, ".config")
					: configuration.ConfigurationFileName);
		}

		private static void Install(bool install, Arguments arguments, IHostServiceConfiguration configuration)
		{
			ColoredConsole.WriteLine(ConsoleColor.Green, "{0} service as '{1}'.", install
				? "Installing"
				: "Uninstalling", configuration.ServiceName);

			using (var installer = new AssemblyInstaller(typeof (Host).Assembly, arguments.CommandLine))
			{
				IDictionary state = new Hashtable();

				installer.UseNewContext = true;

				try
				{
					if (install)
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
									var service = services.OpenSubKey(configuration.ServiceName, true);

									if (service != null)
									{
										service.SetValue("Description", configuration.Description);
										service.SetValue("ImagePath",
											string.Format("{0} /serviceName:{1}{2}{3}",
												service.GetValue("ImagePath"),
												configuration.ServiceName,
												string.IsNullOrEmpty(configuration.Instance)
													? string.Empty
													: string.Format(" /instance:{0}",
														ServiceConfiguration.Instance),
												string.IsNullOrEmpty(configuration.ConfigurationFileName)
													? string.Empty
													: string.Format(" /configurationFileName:{0}",
														ServiceConfiguration.ConfigurationFileName)));
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
					else
					{
						installer.Uninstall(state);
					}
				}
				catch
				{
					try
					{
						installer.Rollback(state);
					}
					catch
					{
					}

					throw;
				}
			}
		}

		private static IHostServiceConfiguration GetHostServiceConfiguration(IHost host, Arguments arguments)
		{
			var serviceName = arguments.Get("serviceName", string.Empty);
			var instance = arguments.Get("instance", string.Empty);

			if (string.IsNullOrEmpty(serviceName))
			{
				serviceName = host.GetType().FullName;
			}

			var displayName = arguments.Get("displayName", string.Empty);

			if (string.IsNullOrEmpty(displayName))
			{
				displayName = GetDefaultDisplayName(serviceName, host);
			}

			if (!string.IsNullOrEmpty(instance))
			{
				serviceName = string.Format("{0}${1}", serviceName, instance);
			}

			var description = arguments.Get("description", string.Empty);

			if (string.IsNullOrEmpty(description))
			{
				description = string.Format("Shuttle.Core.Host for '{0}'.", displayName);
			}

			var configurationFileName = arguments.Get("configurationFileName", string.Empty);

			return new HostServiceConfiguration(host)
			{
				Instance = instance,
				ConfigurationFileName = configurationFileName,
				ServiceName = serviceName,
				DisplayName = displayName,
				Description = description,
				UserName = arguments.Get("username", string.Empty),
				Password = arguments.Get("password", string.Empty),
				StartManually = arguments.Get("startManually", false)
			};
		}

		public static string GetDefaultDisplayName(string serviceName, object hostTypeInstance)
		{
			return string.Format("{0} ({1})", serviceName, hostTypeInstance.GetType().Assembly.GetName().Version);
		}
	}
}