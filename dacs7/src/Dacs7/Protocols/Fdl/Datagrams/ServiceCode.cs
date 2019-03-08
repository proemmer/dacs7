// Copyright (c) Benjamin Proemmer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

namespace Dacs7.Protocols.Fdl
{
    internal enum  ServiceCode : ushort
    {
        /// <summary>
        /// Send Data with Acknowledge
        /// </summary>
        SendDataWithAck = 0x00,

        /// <summary>
        /// Send Data without Acknowledge
        /// </summary>
        SendDataWithoutAck = 0x01,

        /// <summary>
        /// only for FDL-indication !!! (signs received broadcast-telegram)   
        /// </summary>
        SendBroadcast = 0x7f,

        /// <summary>
        ///  Send and Request Data   
        /// </summary>
        SendAndRequestData = 0x03,

        /// <summary>
        ///  Send and Request Data   
        /// </summary>
        CyclicSendAndRequestData = 0x05,

        /// <summary>
        /// 
        /// </summary>
        ReplyUpdateSingleMode = 0x06,

        /// <summary>
        /// 
        /// </summary>
        ReplyUpdateMultiMode = 0x07,

        /// <summary>
        /// 
        /// </summary>
        FdlReadValue = 0x0b,

        /// <summary>
        /// 
        /// </summary>
        FdlWriteValue = 0x0c,

        /// <summary>
        /// 
        /// </summary>
        SapActivate = 0x0e,

        /// <summary>
        /// 
        /// </summary>
        RsapActivate = 0x11,

        /// <summary>
        /// 
        /// </summary>
        SapDeactivate = 0x12,

        /// <summary>
        /// 
        /// </summary>
        FdlReset = 0x13,

        /// <summary>
        /// 
        /// </summary>
        MacReset = 0x15,

        /// <summary>
        /// 
        /// </summary>
        FdlEvent = 0x18,

        /// <summary>
        /// 
        /// </summary>
        LsapStatus = 0x19,

        /// <summary>
        /// 
        /// </summary>
        FdlLifeListCreateRemote = 0x1c,

        /// <summary>
        /// 
        /// </summary>
        FdlLifeListCreateLocal = 0x1b,

        /// <summary>
        /// 
        /// </summary>
        FdlIdent = 0x1c,

        /// <summary>
        /// 
        /// </summary>
        AwaitIndication = 0x1f,

        /// <summary>
        /// 
        /// </summary>
        WithdrawIndication = 0x20,

        /// <summary>
        /// 
        /// </summary>
        LoadRoutingTable = 0x21,

        /// <summary>
        /// 
        /// </summary>
        DeactivateRoutingTable = 0x22,

        /// <summary>
        /// 
        /// </summary>
        GetDirectConnection = 0x23,

    }
}
