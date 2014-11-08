namespace Shuttle.Core.Host
{
    public class HostServiceConfiguration : IHostServiceConfiguration
    {
        public HostServiceConfiguration(IHost host)
        {
            Host = host;
        }

        public IHost Host { get; private set; }

        public string ServiceName { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Instance { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool StartManually { get; set; }
    }
}