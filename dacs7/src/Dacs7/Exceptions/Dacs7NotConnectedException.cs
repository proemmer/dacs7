using System;

namespace Dacs7
{
    public class Dacs7NotConnectedException : Exception
    {

        public Dacs7NotConnectedException() :
            base("Dacs7 has no connection to the plc!")
        {
        }

        public Dacs7NotConnectedException(Exception ex) :
            base("Dacs7 has no connection to the plc!", ex)
        {
        }
    }
}
