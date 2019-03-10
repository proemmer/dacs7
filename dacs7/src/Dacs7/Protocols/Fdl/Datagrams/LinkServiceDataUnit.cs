// Copyright (c) Benjamin Proemmer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using System;

namespace Dacs7.Protocols.Fdl
{

    /// <summary>
    ///  address and length of send-netto-data, exception:                        
    ///   1. csrd                : length means number of POLL-elements       
    ///   2. await_indication    : concatenation of application-blocks and   
    ///   3. withdraw_indication : number of application-blocks 
    /// </summary>
    internal sealed class LinkServiceDataUnit
    {
        public Int32 BufferPtr { get; set; }   // address and length of received netto-data, exception:
        public byte Length { get; set; }         // address and length of received netto-data  max = 255
    }


}
