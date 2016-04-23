Shuttle.Core.Host
=================

The generic host is an executable assembly used to execute code either within a console window or as a Windows service.  Since it can host your code while running in Visual Studio it makes debugging very easy.  No need to attach a debugger to a Windows service.

A typical implementation would be the following:

~~~ c#
using System;
using Shuttle.Core.Host;

namespace Domain.Server
{
	public class DomainHost : IHost, IDisposable
	{
		private volatile bool active = true;
	
		public void Start()
		{
		   while (active)
		   {
				// perform some processing
		   }
		}

		public void Dispose()
		{
			active = false;
		}
	}
}
~~~

When the generic host is executed it searches for all classes that implement the `IHost`.  It needs to find exactly 1 class implementing the interface else it fails with an exception.  If you *do* have more than one type implementing the interface you can specify the interface using an argument:

~~~
/hostType="assembly qualified name"
~~~

In order to debug applications that use the `IHost` interface you would simply need to set the `Shuttle.Core.Host.exe` as the startup application for your project:

![Host Debug Image]({{ site.baseurl }}/assets/images/host-debug.png "Host Debug")

You would probably want to use some thread-base processing but that is up to you.

Notice the `IDisposable` implementation.  Whenever a service is stopped, or `ctrl+c` pressed for a console application, the `IHost` instance is safe-cast to `IDisposable`.  If the host instance implements `IDisposable` the `Dispose` method will be called.

The following command-line arguments are available and can be viewed by running `Shuttle.Core.Host /?`:

~~~
	[/install [/serviceName]]	
		- install the service
		
	[/displayName]				
		- friendly name for the installed service
		
	[/description]				
		- Description for the service
		
	[/hostType]	
		- type implementing IHost that should be used
		
	[/instance]					
		- unique name of the instance you wish to install
		
	[/configurationFileName]
		- an alternate configuration file name to use instead of {IHost.dll}.config

	[/startManually]			
		- specifies that the service should start manually
		
	[/username]					
		- username of the account to use for the service
		
	[/password]]				
		- password of the account to use for the service
		
	- or -
	
	[/uninstall [/serviceName] [/instance]]	
~~~		

## IHost
As mentioned, if no `/hostType` is specified the folder the `Shuttle.Core.Host.exe` is in will be scanned for the class implementing `IHost`.  Should no class, or more than 1 class, be located an exception will be raised.

## Service Name
If no `/serviceName` is specified the full name of the service bus host type will be used along with the version number of the assembly it is contained within.

~~~
	Shuttle.Application.Server.Host (1.0.0.0)
~~~

## Display Name
The default for the `/displayName` is the same value as `/serviceName`, and the description defaults to a generic service bus host description.

## Uninstall

If you set the `/serviceName` and/or `/instance` during installation you will need to specify them when uninstalling as well, e.g.:

~~~
	Shuttle.Core.Host.exe 
		/uninstall 
		/serviceName:"Shuttle.Application.Server" 
		/instance:"Instance5"
~~~

## Example

~~~
Shuttle.Core.Host.exe 
	/install 
	/serviceName:"Shuttle.Application.Server" 
	/displayName:"Shuttle server for the application"
	/description:"Service to handle messages relating to the application" 
	/hostType:"QualifiedNamespace.Host, AssemblyName"
	/username:"domain\hostuser"
	/password:"p@ssw0rd!"
~~~

# API

It is also possible to install and uninstall services that make use of `shuttle-core-host` by using the `WindowsServiceInstaller` class:

~~~ c#
var windowsServiceInstaller = new WindowsServiceInstaller();

var installConfiguration = new InstallConfiguration
{
	ServiceAssemblyPath = @"{path to your service}\Shuttle.Core.Host.exe",
	// more arguments may be specified
};  

windowsServiceInstaller.Install(installConfiguration);

var serviceInstallerConfiguration = new ServiceInstallerConfiguration
{
	ServiceAssemblyPath = @"{path to your service}\Shuttle.Core.Host.exe",
	// more arguments may be specified
};

windowsServiceInstaller.Uninstall(serviceInstallerConfiguration);
~~~