using System;
using System.Collections.Generic;
using System.Text;

namespace Dacs7.Protocols.Fdl
{
    internal class ApplicationBlock
    {
        //Application Block
        public byte Opcode { get; set; }                       // class of communication   (00 = request, 01=confirm, 02=indication)
        public byte Subsystem { get; set; }                    // number of source-task (only necessary for MTK-user !!!!!)
        public ushort Id { get; set; }                         // identification of FDL-USER
        public ServiceCode Service { get; set; }                    // identification of service (00 -> SDA, send data with acknowlege)
        public RemoteAddress LocalAddress { get; set; }        // only for network-connection !!!
        public byte Ssap { get; set; }                         // source-service-access-point
        public byte Dsap { get; set; }                         // destination-service-access-point
        public RemoteAddress RemoteAddress { get; set; }       // address of the remote-station
        public ServiceClass ServiceClass { get; set; }             // priority of service
        public LinkServiceDataUnit Receive1Sdu { get; set; }
        public byte Reserved1{ get; set; }                   // (reserved for FDL !!!!!!!!!!)
        public byte Reserved{ get; set; }                     // (reserved for FDL !!!!!!!!!!)
        public LinkServiceDataUnit Send1Sdu { get; set; }
        public ushort LinkSatus{ get; set; }                   // link-status of service or update_state for srd-indication

        public ushort[] Reserved2{ get; set; }               // for concatenated lists       (reserved for FDL !!!!!!!!!!)
                                                             
    }


}
