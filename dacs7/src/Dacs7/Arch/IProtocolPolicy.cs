using System.Collections.Generic;


namespace Dacs7
{
    public interface IProtocolPolicy
    {
        ExtractionResult ExtractRawMessages(IEnumerable<byte> data);
        IEnumerable<IMessage> Normalize(string origin, IEnumerable<IEnumerable<byte>> rawMessages);
        IEnumerable<IEnumerable<byte>> TranslateToRawMessage(IMessage msg, bool withoutCheck = false);
        IMessage CreateReplyMessage(IMessage message);
        IEnumerable<KeyValuePair<IMessage, IMessage>> MatchCorrelatedMessages(IEnumerable<IMessage> requestMessages, IEnumerable<IMessage> replyMessages);
    }
}
