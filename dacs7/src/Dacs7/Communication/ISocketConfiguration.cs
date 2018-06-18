

namespace Dacs7.Communication
{
    public interface ISocketConfiguration : IConfiguration
    {
        string Hostname { get; set; }
        int ServiceName { get; set; }
        string NetworkAdapter { get; set; }
        bool KeepAlive { get; set; }
    }
}