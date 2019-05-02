// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

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
