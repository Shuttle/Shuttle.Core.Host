using Shuttle.Core.Infrastructure;

namespace Shuttle.Core.Host
{
    public static class ArgumentsExtensions
    {
        public static bool ShouldShowHelp(this Arguments arguments)
        {
            return (arguments.Get("help", false) || arguments.Get("h", false) || arguments.Get("?", false));
        }
        
    }
}