using InacS7Core.Helper;
using System.Text;

namespace InacS7Core.Protocols.RFC1006
{
    public class Tsap
    {
        // Client Connection Type for SPLC
        //const word CONNTYPE_PG = 0x01;  // Connect to the PLC as a PG
        //const word CONNTYPE_OP = 0x02;  // Connect to the PLC as an OP
        //const word CONNTYPE_BASIC = 0x03;  // Basic connection 
        //RemoteTSAP = (ConnectionType<<8)+(Rack*0x20)+Slot;

        public string Local { get; private set; }
        public string Remote { get; private set; }


        public Tsap(string aRemoteTsap, string aLocalTsap)
        {
            Remote = aRemoteTsap.StartsWith("0x") ? Encoding.ASCII.GetString(aRemoteTsap.Substring(2).HexGetBytes()) : aRemoteTsap;
            Local = aLocalTsap.StartsWith("0x") ? Encoding.ASCII.GetString(aLocalTsap.Substring(2).HexGetBytes()) : aLocalTsap;
        }
    }
}
