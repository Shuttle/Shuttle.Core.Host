using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using Shuttle.Core.Infrastructure;

namespace Shuttle.Core.Host
{
	internal class HostFactory
	{
		public IHost Create(Arguments arguments)
		{
			Guard.AgainstNull(arguments, "arguments");

			var hostType = GetHostType(arguments);

			hostType.AssertDefaultConstructor(
				string.Format("Service bus host type '{0}' implementing IHost must have a default constructor.", hostType.FullName));

			return (IHost) Activator.CreateInstance(hostType);
		}

		private Type GetHostType(Arguments arguments)
		{
			var hostType = arguments.Get("HostType", string.Empty);

			if (!string.IsNullOrEmpty(hostType))
			{
				var type = Type.GetType(hostType, false);

				if (type == null)
				{
					throw new ConfigurationErrorsException(
						string.Format(
							"Could not load type '{0}' specified for the 'serviceBusHostType' command line argument.",
							hostType));
				}

				return type;
			}

			var hostTypes = ScanAssembliesForHostTypes();

			if (hostTypes.Count() == 0)
			{
				throw new InvalidOperationException(string.Format(
					"No type implementing IHost could be found in the scanned assemblies.\r\n " +
					"This could happen when the Shuttle.Core.Host fails to load your assembly containing the type implementing IHost.\r\n\r\n" +
					"Try specifying the type explicitly on the command line using the 'HostType' option.\r\n\r\n" +
					"One possibility is that the assembly that contains the IHost implementation, or one of it's dependancies, cannot be loaded.  Please check the log messages to establish if this is the case.\r\n\r\n" +
					"Another possibility is that the framework Shuttle.Core.Host is running under ({0}) is older than the one you used to implement IHost.\r\n\r\n" +
					"Assemblies scanned in folder: {1}", Environment.Version, AppDomain.CurrentDomain.BaseDirectory));
			}

			if (hostTypes.Count() > 1)
			{
				throw new InvalidOperationException(
					"The Shuttle.Core.Host doesn't support hosting of multiple IHost type implementations. " +
					"Types found: " +
					string.Join(", ",
					            hostTypes.Select(e => e.AssemblyQualifiedName).ToArray()) +
					" You may have some old assemblies in your runtime directory." +
					" Try right-clicking your VS project, and selecting 'Clean'."
					);
			}

			return hostTypes.First();
		}

		private IEnumerable<Type> ScanAssembliesForHostTypes()
		{
			var result = new List<Type>();

			var interfaceType = typeof (IHost);

			foreach (var assembly in GetAssembliesToScan())
			{
				result.AddRange(
					assembly.GetTypes().Where(candidate => interfaceType.IsAssignableFrom(candidate) && candidate != interfaceType));
			}

			return result;
		}

		public IEnumerable<Assembly> GetAssembliesToScan()
		{
			var result = new List<Assembly>();

			var files =
				Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll", SearchOption.AllDirectories)
					.Union(Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.exe",
					                          SearchOption.AllDirectories));

			Log.For(this).Trace(string.Format("Scanning {0} assemblies for type implementing IHost.", files.Count()));

			foreach (var file in files)
			{
				Assembly assembly;

				try
				{
					Log.For(this).Trace(string.Format("Scanning assembly '{0}'.", file));

					assembly = Assembly.LoadFrom(file);

					assembly.GetTypes();
				}
				catch (Exception ex)
				{
					var reflection = ex as ReflectionTypeLoadException;

					if (reflection != null)
					{
						foreach (var exception in reflection.LoaderExceptions)
						{
							Log.For(this).Trace(string.Format("'{0}'.", exception.AllMessages()));
						}
					}
					else
					{
						Log.For(this).Trace(string.Format("{0}: '{1}'.", ex.GetType(), ex.AllMessages()));
					}

					continue;
				}

				result.Add(assembly);
			}

			return result;
		}
	}
}