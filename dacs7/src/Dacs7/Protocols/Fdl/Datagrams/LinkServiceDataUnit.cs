using System;

namespace Dacs7.Protocols.Fdl
{

    /// <summary>
    ///  address and length of send-netto-data, exception:                        
    ///   1. csrd                : length means number of POLL-elements       
    ///   2. await_indication    : concatenation of application-blocks and   
    ///   3. withdraw_indication : number of application-blocks 
    /// </summary>
    internal class LinkServiceDataUnit
    {
        public Int32 BufferPtr { get; set; }   // address and length of received netto-data, exception:
        public byte Length { get; set; }         // address and length of received netto-data  max = 255
    }


}
