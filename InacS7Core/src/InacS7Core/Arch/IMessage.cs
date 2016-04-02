using System.Collections.Generic;

namespace InacS7Core.Arch
{
    public interface IMessage
    {
        string GetID();
        IEnumerable<byte> GetRawMessage();
        object GetPayload();
        int GetAge();
        string GetOrigin();
        IMessage GetReplyMessage();
        Dictionary<string, object> Attributes { get; }
        T GetAttribute<T>(string attributeName, T defaultValue);
        void SetAttribute(string attributeName, object attributeValue);
        string ToString();
        //void RemoveFromChannel();
    }
}
