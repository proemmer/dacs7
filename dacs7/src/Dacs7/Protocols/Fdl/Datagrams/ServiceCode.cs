namespace Dacs7.Protocols.Fdl
{
    internal enum  ServiceCode : ushort
    {
        FdlReadValue,
        SapActivate,
        RsapActivate,
        SapDeactivate,
        FdlLifeListCreateLocal,
        FdlIdent,
        FdlEvent,
        AwaitIndication,
        WithdrawIndication,
        LsapStatus,
        FdlLifeListCreateRemote
    }
}
