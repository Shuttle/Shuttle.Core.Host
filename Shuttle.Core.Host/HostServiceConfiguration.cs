using Shuttle.Core.Infrastructure;

namespace Shuttle.Core.Host
{
	public class HostServiceConfiguration
	{
		public IHost Host { get; private set; }

		public HostServiceConfiguration(IHost host)
		{
			Guard.AgainstNull(host, "host");

			Host = host;
		}

		public string ServiceName { get; set; }
		public string ConfigurationFileName { get; set; }

		public static HostServiceConfiguration FromArguments(IHost host, Arguments arguments)
		{
			return new HostServiceConfiguration(host)
			{
				ServiceName = arguments.Get("serviceName", host.GetType().FullName),
				ConfigurationFileName = arguments.Get("configurationFileName", string.Empty)
			};
		}
	}
}