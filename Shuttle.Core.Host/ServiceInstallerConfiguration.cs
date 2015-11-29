using System;
using System.Reflection;
using Shuttle.Core.Infrastructure;

namespace Shuttle.Core.Host
{
    public class ServiceInstallerConfiguration : MarshalByRefObject
    {
        public ServiceInstallerConfiguration()
        {
        }

        public ServiceInstallerConfiguration(Arguments arguments)
        {
            ServiceName = arguments.Get("serviceName", string.Empty);
            Instance = arguments.Get("instance", string.Empty);
        }

        public string ServiceAssemblyPath { get; set; }
        public string ServiceName { get; set; }
        public string Instance { get; set; }
        public string HostTypeAssemblyQualifiedName { get; set; }

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