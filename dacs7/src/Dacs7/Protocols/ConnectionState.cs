namespace Dacs7.Protocols
{
    internal enum ConnectionState
    {
        Closed,
        PendingOpenTransport,
        TransportOpened,
        PendingOpenPlc,
        Opened
    }
}
