using System;
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
				Log.Assign(new ConsoleLog(typeof(Program)) { LogLevel = LogLevel.Trace });
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

				new Host().RunService(new HostFactory().Create(arguments), arguments);
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
					Log.Fatal(string.Format("[UNHANDLED EXCEPTION] : exception = {0}", ex.AllMessages()));

					throw;
				}
			}
		}

		private static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			var ex = e.ExceptionObject as Exception;

			Log.Fatal(string.Format("[UNHANDLED EXCEPTION] : exception = {0}", ex != null ? ex.AllMessages() : "(the exception object is null)"));
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