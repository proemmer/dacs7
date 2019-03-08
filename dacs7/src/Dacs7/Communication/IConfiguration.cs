namespace Dacs7.Communication
{
    public interface IConfiguration
    {
        int ReceiveBufferSize { get; set; }
        int AutoconnectTime { get; set; }
    }
}
