namespace Dacs7
{
    public interface IReplyMessage
    {
        string GetCorrelationID();
        string GetReturnAddress();
    }
}
