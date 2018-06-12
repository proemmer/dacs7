using System;

namespace Dacs7
{
    public class Dacs7NotConnectedException : Exception
    {

        public Dacs7NotConnectedException() :
            base("Dacs7 has no connection to the plc!")
        {
        }
    }
}
