namespace Dacs7.Protocols
{
    internal enum ConnectionState
    {
        Closed,
        PendingOpenRfc1006,
        Rfc1006Opened,
        PendingOpenPlc,
        Opened
    }
}
