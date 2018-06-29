namespace Dacs7.Communication
{
    public class S7ApiConfiguration : IConfiguration
    {
        public string CpDescription { get; set; }
        public int ReceiveBufferSize { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public int AutoconnectTime { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
    }
}