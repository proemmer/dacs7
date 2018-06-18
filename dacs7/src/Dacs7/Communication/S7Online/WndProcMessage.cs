using System.Runtime.InteropServices;

namespace Dacs7.Communication.S7Online
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    internal class WndProcMessage
    {
        public int pdulength;
        public byte[] pdu;
    }
}
