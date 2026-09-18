# Changelog

## 2.2.7

Bugfix release. No public API was removed or renamed, and the client's behaviour is unchanged except for the bugs fixed below.

### Client

- **Replies split across TCP reads no longer corrupt the connection.** Before, after a reply arrived in more than one TCP read, the following replies on that connection were parsed from stale data and ran into timeouts until the connection was dropped. This mostly affected VPN/WAN links and many parallel jobs. On a LAN, where replies arrive complete, the receive loop behaves exactly as before.
- **`ConnectAsync` no longer times out now and then when the PLC answers very fast.** The client now updates its connection state before it sends the connection request, not after. Otherwise a fast connection confirm could arrive first, get ignored, and `ConnectAsync` ran into its timeout (about 1 in 100 connects under load).
- **A connection confirm with an unknown parameter no longer hangs the receive thread.** Unknown COTP parameters are now skipped. Before, the parser looped forever at 100% CPU.
- **`ReadBlocksOfTypeAsync` now returns the last block of the list, and `ReadBlocksCountAsync` now returns the count of the last block type in the reply.** Both parsers stopped one entry early. An unknown block type in the count reply no longer shifts the parsing of the following entries.
- Fixed a `NullReferenceException` when an ack for an alarm indication subscription is received.

### Server (`Dacs7Server`)

- **Bit requests.**
  - `RequestItem.TransportSize` is now public, so data providers can distinguish bit requests from byte requests. For a bit request, `Offset` is the bit address (byte offset * 8 + bit number).
  - `RequestItem.ToTag()` now renders bit requests as bit tags. For example, a request for bit 3 of byte 2 becomes `DB100.2,x3,1`; before it was `DB100.19,B,1`. Other requests render as before. This also fixes bit access through `RelayPlcDataProvider`.
- **Connections accepted under load are no longer dropped.** A client's connection request could be processed before the connection was fully set up, so the connection confirm was never sent.
- **A connection that fails during setup is closed and logged as a warning.** It no longer stops the server from accepting further connections.
- **`DisconnectAsync` no longer throws "Collection was modified".** The list of client connections is now synchronized. On stop, the client connections are closed before the listener.
- **Stopping the server no longer logs `Critical` or `Error`.**
- **The server no longer tries to reconnect to clients which disconnected, and closes their connection.** Before, it kept dialing the client's old address and port every 5 seconds, even after the server was stopped.
- **The server always answers read, write and communication setup jobs.** If the data provider throws, or returns a different number of results than requested, every item is answered with `HardwareFault` and the error is logged. Before, the client got no answer and ran into its timeout.
- **A connection request with an unknown parameter is now accepted.** The unknown COTP parameter is skipped. Before, it hung the server's receive thread.

## 2.2.6

- read item size calculation bug fixed
- Server bithandling fixed
- Added Handling of ACKDatagram (e.g. in case of Putget deactivated)
- removed snap7 testserver
