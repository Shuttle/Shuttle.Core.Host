using System;
using System.Reflection;
using Shuttle.Core.Infrastructure;

namespace Shuttle.Core.Host
{
	public class ServiceInstallerConfiguration
	{
		public Assembly ServiceAssembly { get; set; }
		public string ServiceName { get; set; }
		public string Instance { get; set; }

		public static ServiceInstallerConfiguration FromArguments(Arguments arguments)
		{
			return new ServiceInstallerConfiguration
			{
				ServiceName = arguments.Get("serviceName", String.Empty),
				Instance = arguments.Get("instance", String.Empty)
			};
		}

		public virtual bool HostTypeRequired
		{
			get { return string.IsNullOrEmpty(ServiceName); }
		}

		public virtual void ApplyInvariants()
		{
			Guard.Against<Exception>(string.IsNullOrEmpty(ServiceName), "ServiceName may not be empty.");
		}

		public string InstancedServiceName()
		{
			return string.Concat(ServiceName, string.IsNullOrEmpty(Instance)
				? string.Empty
				: string.Format("${0}", Instance));
		}

		public virtual void ApplyHostType(Type type)
		{
			if (string.IsNullOrEmpty(ServiceName))
			{
				ServiceName = type.FullName;
			}
		}
	}
}