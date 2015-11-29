using Shuttle.Core.Infrastructure;

namespace Shuttle.Core.Host
{
    public static class ArgumentsExtensions
    {
        public static bool ShouldShowHelp(this Arguments arguments)
        {
            Guard.AgainstNull(arguments, "arguments");

            return (arguments.Get("help", false) || arguments.Get("h", false) || arguments.Get("?", false));
        }

        public static string GetHostType(this Arguments arguments)
        {
            Guard.AgainstNull(arguments, "arguments");

            return arguments.Get("HostType", string.Empty);
        }
    }
}