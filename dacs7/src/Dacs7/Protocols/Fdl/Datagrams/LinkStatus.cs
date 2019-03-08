// Copyright (c) Benjamin Proemmer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

namespace Dacs7.Protocols.Fdl
{
    internal enum LinkStatus : ushort
    {
        Ok = 0x00,               // ACK. positive                                                        
        Ue = 0x01,               // ACK. negative:   remote-USER/FDL interface error                     
        Rr = 0x02,               // ACK. negative:   no remote resource available                        
        Rs = 0x03,               // ACK. negative:   service or rem_add at remote-lsap not activated     
        Dl = 0x08,               // response-data (l_sdu) low available                                  
        Nr = 0x09,               // ACK. negative:   no response-data at remote-FDL available            
        Dh = 0x0a,               // response-data (l_sdu) high available                                 
        Rdl = 0x0c,               // response-data (l_sdu) low available, but negative-ACK for send-data  
        Rdh = 0x0d,               // response-data (l_sdu) high available, but negative-ACK for send-data 
        Ls = 0x10,               // service not activated at local sap                                   
        Na = 0x11,               // no reaction (ACK/RES) from remote-station                            
        Ds = 0x12,               // local FDL/PHY not in token-ring                                      
        No = 0x13,               // ACK. negative:   not ok (different meanings dependant on service)    
        Lr = 0x14,               // resource of local FDL not available                                  
        Iv = 0x15,               // invalid parameter in request                                         
        Lo = 0x20,               // LOw-prior response-data are sent at this srd                         
        Hi = 0x21,               // HIgh-prior response-data are sent at this srd                        
        NoData = 0x22                // NO-DATA are sent at this srd                                         
    };
}
