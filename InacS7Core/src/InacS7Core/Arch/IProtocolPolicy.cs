using System.Collections.Generic;


namespace InacS7Core.Arch
{
    public interface IProtocolPolicy
    {
        ExtractionResult ExtractRawMessages(IEnumerable<byte> data);
        IEnumerable<IMessage> Normalize(string origin, IEnumerable<object> rawMessages);
        object TranslateToRawMessage(IMessage msg);
        IMessage CreateReplyMessage(IMessage message);
        IEnumerable<KeyValuePair<IMessage, IMessage>> MatchCorrelatedMessages(IEnumerable<IMessage> requestMessages, IEnumerable<IMessage> replyMessages);
    }
}
