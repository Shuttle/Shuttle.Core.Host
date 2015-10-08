using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Shuttle.Core.Infrastructure;

namespace Shuttle.Core.Host
{
	public class Program
	{
		public static void Main(params string[] args)
		{
			if (Environment.UserInteractive)
			{
				Log.Assign(new ConsoleLog(typeof (Program)) {LogLevel = LogLevel.Trace});
			}

			AppDomain.CurrentDomain.UnhandledException += UnhandledException;

			try
			{
				var arguments = new Arguments(args);

				if (arguments.ShouldShowHelp())
				{
					ShowHelp();

					return;
				}

				var install = arguments.Get("install", String.Empty);
				var uninstall = arguments.Get("uninstall", String.Empty);

				if (!String.IsNullOrEmpty(install) && !String.IsNullOrEmpty(uninstall))
				{
					throw new ConfigurationErrorsException("Cannot specify /install and /uninstall together.");
				}

				var hostFactory = new HostFactory();

				if (!String.IsNullOrEmpty(uninstall))
				{
					var serviceConfiguration = ServiceInstallerConfiguration.FromArguments(arguments);

					if (serviceConfiguration.HostTypeRequired)
					{
						serviceConfiguration.ApplyHostType(hostFactory.GetHostType(arguments));
					}

					HostServiceInstaller.Assign(serviceConfiguration);

					new WindowsServiceInstaller().Uninstall(serviceConfiguration);
				}
				else
				{
					if (!String.IsNullOrEmpty(install))
					{
						var installConfiguration = InstallConfiguration.FromArguments(arguments);

						if (installConfiguration.HostTypeRequired)
						{
							installConfiguration.ApplyHostType(hostFactory.GetHostType(arguments));
						}

						HostServiceInstaller.Assign(installConfiguration);

						new WindowsServiceInstaller().Install(installConfiguration);
					}
					else
					{
						new Host().RunService(HostServiceConfiguration.FromArguments(hostFactory.Create(arguments), arguments));
					}
				}
			}
			catch (Exception ex)
			{
				if (Environment.UserInteractive)
				{
					ColoredConsole.WriteLine(ConsoleColor.Red, ex.AllMessages());

					Console.WriteLine();
					ColoredConsole.WriteLine(ConsoleColor.Gray, "Press any key to close...");
					Console.ReadKey();
				}
				else
				{
					Log.Fatal(String.Format("[UNHANDLED EXCEPTION] : exception = {0}", ex.AllMessages()));

					throw;
				}
			}
		}

		private static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			var ex = e.ExceptionObject as Exception;

			Log.Fatal(String.Format("[UNHANDLED EXCEPTION] : exception = {0}",
				ex != null ? ex.AllMessages() : "(the exception object is null)"));
		}

		protected static void ShowHelp()
		{
			try
			{
				using (
					var stream =
						Assembly.GetCallingAssembly().GetManifestResourceStream("Shuttle.Core.Host.Content.Help.txt"))
				{
					if (stream == null)
					{
						Console.WriteLine("Error retrieving help content stream.");

						return;
					}

					Console.WriteLine(new StreamReader(stream).ReadToEnd());
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
		}
	}
}