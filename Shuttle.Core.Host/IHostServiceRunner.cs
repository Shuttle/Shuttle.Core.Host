namespace Shuttle.Core.Host
{
    public interface IHostServiceRunner
    {
        void Run(IHostServiceConfiguration configuration);
    }
}