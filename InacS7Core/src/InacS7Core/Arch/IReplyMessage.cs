namespace InacS7Core.Arch
{
    public interface IReplyMessage
    {
        string GetCorrelationID();
        string GetReturnAddress();
    }
}
