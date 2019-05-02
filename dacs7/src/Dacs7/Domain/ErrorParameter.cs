// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using Dacs7.Helper;

namespace Dacs7
{
    public enum ErrorParameter : ushort
    {
        [Description("No error")]
        NoError = 0x0000,

        [Description("Invalid block type number")]
        InvalidBlockTypeNum = 0x0110,

        [Description("Invalid parameter")]
        InvalidParam = 0x0112,

        [Description("PG resource error")]
        PgResourceError = 0x011A,

        [Description("PLC resource error")]
        PlcResourceError = 0x011B,

        [Description("Protocol error")]
        ProtocolError = 0x011C,

        [Description("User buffer too short")]
        UserBufferTooShort = 0x011F,

        [Description("Request error")]
        ReqIniErr = 0x0141,

        [Description("Version mismatch")]
        VersionMismatch = 0x01C0,

        [Description("Not implemented")]
        NotImplemented = 0x01F0,

        [Description("L7 invalid CPU state")]
        L7InvalidCpuState = 0x8001,

        [Description("L7 PDU size error")]
        L7PduSizeErr = 0x8500,

        [Description("L7 invalid SZL ID")]
        L7InvalidSzlID = 0xD401,

        [Description("L7 invalid index")]
        L7InvalidIndex = 0xD402,

        [Description("L7 DGS Connection already announced")]
        L7DgsConnAlreadyAnnou = 0xD403,

        [Description("L7 Max user NB")]
        L7MaxUserNb = 0xD404,

        [Description("L7 DGS function parameter syntax error")]
        L7DgsFktParSyntaxErr = 0xD405,

        [Description("L7 no info")]
        L7NoInfo = 0xD406,

        [Description("L7 PRT function parameter syntax error")]
        L7PrtFktParSyntaxErr = 0xD601,

        [Description("L7 invalid variable address")]
        L7InvalidVarAddr = 0xD801,

        [Description("L7 unknown request")]
        L7UnknownReq = 0xD802,

        [Description("L7 invalid request status")]
        L7InvalidReqStatus = 0xD803
    }



}