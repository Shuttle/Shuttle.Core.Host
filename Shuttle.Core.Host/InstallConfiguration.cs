using System;
using Shuttle.Core.Infrastructure;

namespace Shuttle.Core.Host
{
	public class InstallConfiguration : ServiceInstallerConfiguration
	{
		private string _displayName;
		private string _description;

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
					: String.Format("Shuttle.Core.Host for '{0}'.", DisplayName);
			}
			set { _description = value; }
		}

		public string ConfigurationFileName { get; set; }
		public string HostTypeFullName { get; set; }
		public string HostTypeAssemblyQualifiedName { get; set; }
		public string UserName { get; set; }
		public string Password { get; set; }
		public bool StartManually { get; set; }

		public new static InstallConfiguration FromArguments(Arguments arguments)
		{
			return new InstallConfiguration
			{
				ServiceName = arguments.Get("serviceName", String.Empty),
				Instance = arguments.Get("instance", String.Empty),
				DisplayName = arguments.Get("displayName", String.Empty),
				Description = arguments.Get("description", String.Empty),
				ConfigurationFileName = arguments.Get("configurationFileName", String.Empty)
			};
		}

		public override void ApplyInvariants()
		{
			base.ApplyInvariants();

			Guard.Against<Exception>(string.IsNullOrEmpty(HostTypeFullName), "HostTypeFullName may not be empty.");
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

		public override bool HostTypeRequired
		{
			get
			{
				return base.HostTypeRequired || string.IsNullOrEmpty(HostTypeAssemblyQualifiedName) ||
				       string.IsNullOrEmpty(HostTypeFullName);
			}
		}

		public override void ApplyHostType(Type type)
		{
			base.ApplyHostType(type);

			HostTypeAssemblyQualifiedName = type.AssemblyQualifiedName;
			HostTypeFullName = type.FullName;
		}
	}
}