using InacS7Core.Helper;

namespace InacS7Core.Domain
{
    public enum ErrorClass : byte
    {
        [Description("No error")]
        None = 0x00,

        [Description("Application relationship")]
        ApplicationRelationship = 0x81,

        [Description("Object definition")]
        ObjectDefinition = 0x82,

        [Description("No resources available")]
        Resources = 0x83,

        [Description("Error on service processing")]
        Service = 0x84,

        [Description("Error on supplies")]
        Supplies = 0x85,

        [Description("Access error")]
        Access = 0x87
    }

    public enum ErrorParameter : ushort
    {
        [Description("No error")]
        NoError             =        0x0000,

        [Description("Invalid block type number")]
        InvalidBlockTypeNum    =   0x0110,

        [Description("Invalid parameter")]
        InvalidParam             =   0x0112,

        [Description("PG resource error")]
        PgResourceError        =    0x011A,

        [Description("PLC resource error")]
        PlcResourceError       =    0x011B,

        [Description("Protocol error")]
        ProtocolError          =     0x011C,

        [Description("User buffer too short")]
        UserBufferTooShort   =     0x011F,

        [Description("Request error")]
        ReqIniErr            =      0x0141,

        [Description("Version mismatch")]
        VersionMismatch       =      0x01C0,

        [Description("Not implemented")]
        NotImplemented        =      0x01F0,

        [Description("L7 invalid CPU state")]
        L7InvalidCpuState    =     0x8001,

        [Description("L7 PDU size error")]
        L7PduSizeErr         =     0x8500,

        [Description("L7 invalid SZL ID")]
        L7InvalidSzlID       =     0xD401,

        [Description("L7 invalid index")]
        L7InvalidIndex         =    0xD402,

        [Description("L7 DGS Connection already announced")]
        L7DgsConnAlreadyAnnou =   0xD403,

        [Description("L7 Max user NB")]
        L7MaxUserNb           =    0xD404,

        [Description("L7 DGS function parameter syntax error")]
        L7DgsFktParSyntaxErr =   0xD405,

        [Description("L7 no info")]
        L7NoInfo                =   0xD406,

        [Description("L7 PRT function parameter syntax error")]
        L7PrtFktParSyntaxErr  =  0xD601,

        [Description("L7 invalid variable address")]
        L7InvalidVarAddr       =   0xD801,

        [Description("L7 unknown request")]
        L7UnknownReq           =    0xD802,

        [Description("L7 invalid request status")]
        L7InvalidReqStatus =    0xD803
    }

    public enum ItemResponseRetVaulue : byte
    {
        Reserved = 0x00,
        [Description("Hardware error")]
        HardwareFault = 0x01,

        [Description("Accessing the object not allowed")]
        AccessFault = 0x03,

        [Description("Invalid address")]
        OutOfRange = 0x05,       /* the desired address is beyond limit for this PLC */

        [Description("Data type not supported")]
        NotSupported = 0x06,     /* Type is not supported */

        [Description("Data type inconsistent")]
        SizeMismatch = 0x07,     /* Data type inconsistent */

        [Description("Object does not exist")]
        DataError = 0x0a,        /* the desired item is not available in the PLC, e.g. when trying to read a non existing DB*/

        [Description("Success")]
        DataOk = 0xFF,
    }
}