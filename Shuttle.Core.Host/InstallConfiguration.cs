using System;
using Shuttle.Core.Infrastructure;

namespace Shuttle.Core.Host
{
    public class InstallConfiguration : ServiceInstallerConfiguration
    {
        private string _description;
        private string _displayName;

        public InstallConfiguration()
        {
        }

        public InstallConfiguration(Arguments arguments)
            : base(arguments)
        {
            DisplayName = arguments.Get("displayName", string.Empty);
            Description = arguments.Get("description", string.Empty);
            ConfigurationFileName = arguments.Get("configurationFileName", string.Empty);
            UserName = arguments.Get("username", string.Empty);
            Password = arguments.Get("password", string.Empty);
        }

        public string DisplayName
        {
            get
            {
                return !string.IsNullOrEmpty(_displayName)
                    ? _displayName
                    : InstancedServiceName();
            }
            set { _displayName = value; }
        }

        public string Description
        {
            get
            {
                return !string.IsNullOrEmpty(_description)
                    ? _description
                    : string.Format("Shuttle.Core.Host for '{0}'.", DisplayName);
            }
            set { _description = value; }
        }

        public string ConfigurationFileName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool StartManually { get; set; }

        public override bool HostTypeRequired
        {
            get
            {
                return base.HostTypeRequired || string.IsNullOrEmpty(HostTypeAssemblyQualifiedName);
            }
        }

        public override void ApplyInvariants()
        {
            base.ApplyInvariants();

            Guard.Against<Exception>(string.IsNullOrEmpty(HostTypeAssemblyQualifiedName),
                "HostTypeAssemblyQualifiedName may not be empty.");
        }

        public string CommandLineArguments()
        {
            return string.Format("/serviceName:\"{0}\"{1}{2}",
                InstancedServiceName(),
                string.IsNullOrEmpty(ConfigurationFileName)
                    ? string.Empty
                    : string.Format(" /configurationFileName:\"{0}\"", ConfigurationFileName),
                string.Format(" /hostType:\"{0}\"", HostTypeAssemblyQualifiedName));
        }

        public override void ApplyHostType(Type type)
        {
            base.ApplyHostType(type);

            HostTypeAssemblyQualifiedName = type.AssemblyQualifiedName;
        }
    }
}