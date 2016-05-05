namespace Dacs7.Arch
{
    public interface IReplyMessage
    {
        string GetCorrelationID();
        string GetReturnAddress();
    }
}
