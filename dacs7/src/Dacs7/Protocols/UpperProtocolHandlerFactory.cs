using Dacs7.Arch;
using Dacs7.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dacs7.Protocols
{
    internal class UpperProtocolHandlerFactory
    {
        private readonly Dictionary<string, IUpperProtocolHandler> UpperProtocolHandler = new Dictionary<string, IUpperProtocolHandler>();
        public void AddUpperProtocolHandler(IUpperProtocolHandler handler)
        {
            var protName = handler.GetType().Name;
            if (!UpperProtocolHandler.ContainsKey(protName))
                UpperProtocolHandler.Add(protName, handler);
            else
                throw new ArgumentException("A protocol with this name already exits!");
        }

        public bool RemoveProtocolHandler(string name)
        {
            return UpperProtocolHandler.Remove(name);
        }

        public byte[] AddUpperProtocolFrame(byte[] txData)
        {
            return UpperProtocolHandler.Values.Aggregate(txData, (current, upperProtocolHandler) => upperProtocolHandler.AddUpperProtocolFrame(current));
        }

        public IEnumerable<byte[]> RemoveUpperProtocolFrame(IEnumerable<byte> rxData, int count)
        {
            var dataBuffer = new List<byte[]> { rxData.Take(count).ToArray() };
            var protocolHandlerBuffer = new List<byte[]>();
            foreach (var upperProtocolHandler in UpperProtocolHandler.Values)
            {
                foreach (var buffer in dataBuffer)
                {
                    protocolHandlerBuffer = upperProtocolHandler.RemoveUpperProtocolFrame(buffer, buffer.Length).ToList();
                }
                dataBuffer = protocolHandlerBuffer;
            }
            return dataBuffer;
        }

        public void OnConnected()
        {
            UpperProtocolHandler.Values.ForEach(ph => ph.Connect());
        }

        public void OnShutdown()
        {
            UpperProtocolHandler.Values.ForEach(ph => ph.Shutdown());
        }

    }
}
