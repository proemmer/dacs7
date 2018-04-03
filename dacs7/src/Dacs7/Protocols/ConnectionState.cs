namespace Dacs7.Protocols
{
    public enum ConnectionState
    {
        Closed,
        PendingOpenRfc1006,
        Rfc1006Opened,
        PendingOpenPlc,
        Opened
    }
}
