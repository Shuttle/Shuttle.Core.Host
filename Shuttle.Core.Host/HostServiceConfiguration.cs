using System;
using Shuttle.Core.Infrastructure;

namespace Shuttle.Core.Host
{
    public class HostServiceConfiguration : IHostServiceConfiguration
    {
	    private readonly Func<Type> _getHostTypeDelegate;

	    public HostServiceConfiguration(Func<Type> getHostTypeDelegate)
	    {
			Guard.AgainstNull(getHostTypeDelegate, "getHostTypeDelegate");

		    _getHostTypeDelegate = getHostTypeDelegate;
	    }

	    public string HostTypeAssemblyQualifiedName()
	    {
		    return _getHostTypeDelegate.Invoke().AssemblyQualifiedName;
	    }

	    public string ConfigurationFileName { get; set; }
	    public string ServiceName { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Instance { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool StartManually { get; set; }
    }
}