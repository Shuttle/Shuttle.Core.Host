namespace Shuttle.Core.Host
{
    public interface IHostServiceConfiguration
    {
	    string HostTypeAssemblyQualifiedName();
		string ConfigurationFileName { get; set; }
        string ServiceName { get; set; }
        string DisplayName { get; set; }
        string Description { get; set; }
        string Instance { get; set; }
        string UserName { get; set; }
        string Password { get; set; }
        bool StartManually { get; set; }
    }
}