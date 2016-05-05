using System;

namespace Dacs7.Arch
{
    public class ReplyMessage : Message, IReplyMessage
    {
        private readonly string _correlationID;
        private readonly string _returnAddress = string.Empty;

        public static IMessage Create(IMessage message)
        {
            return new ReplyMessage(message.GetOrigin(), message.GetID());
        }

        private ReplyMessage(string returnAddress, string correlationID)
        {
            this._returnAddress = returnAddress;
            this._correlationID = correlationID;
        }

        public string GetCorrelationID()
        {
            return _correlationID;
        }

        public string GetReturnAddress()
        {
            return _returnAddress;
        }

        public override string ToString()
        {
            return String.Format("ReplyMessage {0} for {1}.", GetID(), GetCorrelationID());
        }
    }
}
