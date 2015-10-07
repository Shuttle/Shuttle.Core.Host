using System;
using System.Configuration;
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
				Type hostType = null;

				var hostServiceConfiguration = GetHostServiceConfiguration(arguments,
					() => hostType ?? (hostType = hostFactory.GetHostType(arguments)));

				if (!String.IsNullOrEmpty(uninstall))
				{
					Host.Uninstall(hostServiceConfiguration);
				}
				else
				{
					if (!String.IsNullOrEmpty(install))
					{
						Host.Install(hostServiceConfiguration);
					}
					else
					{
						new Host().RunService(hostFactory.Create(arguments), hostServiceConfiguration);
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

		private static IHostServiceConfiguration GetHostServiceConfiguration(Arguments arguments,
			Func<Type> getHostTypeDelegate)
		{
			var serviceName = arguments.Get("serviceName", String.Empty);
			var instance = arguments.Get("instance", String.Empty);

			if (String.IsNullOrEmpty(serviceName))
			{
				serviceName = getHostTypeDelegate.Invoke().FullName;
			}

			var displayName = arguments.Get("displayName", String.Empty);

			if (String.IsNullOrEmpty(displayName))
			{
				displayName = string.Format("{0} ({1})", serviceName, getHostTypeDelegate.Invoke().Assembly.GetName().Version);
			}

			if (!String.IsNullOrEmpty(instance))
			{
				serviceName = String.Format("{0}${1}", serviceName, instance);
			}

			var description = arguments.Get("description", String.Empty);

			if (String.IsNullOrEmpty(description))
			{
				description = String.Format("Shuttle.Core.Host for '{0}'.", displayName);
			}

			var configurationFileName = arguments.Get("configurationFileName", String.Empty);

			var hostServiceConfiguration = new HostServiceConfiguration(getHostTypeDelegate)
			{
				Instance = instance,
				ConfigurationFileName = configurationFileName,
				ServiceName = serviceName,
				DisplayName = displayName,
				Description = description,
				UserName = arguments.Get("username", String.Empty),
				Password = arguments.Get("password", String.Empty),
				StartManually = arguments.Get("startManually", false)
			};

			HostServiceInstaller.Assign(hostServiceConfiguration);

			return hostServiceConfiguration;
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