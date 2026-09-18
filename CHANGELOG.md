# Changelog

## 2.3.0

This release changes behaviour only where 2.2.7 failed or returned wrong data. Every read and write that worked with 2.2.7 sends exactly the same requests to the PLC as before.

### Client

- **Arrays of INT, WORD, DINT, DWORD, REAL, LINT and LWORD are sized in bytes.** The size of such an array was counted in elements instead of bytes. As a result:
  - Reads and writes larger than a PDU were not split correctly and failed with an exception.
  - Requests that exceeded the PDU were rejected by the PLC (error class 0x85). A write like that also made the PLC close the connection.
  
  Now these items are split into parts that fit into the PDU.
- **If one part of a split item fails, the item reports the error.** Before, the return code of the last part was used.
- **New tag types:** `li` (LINT), `si` (SINT), `ws` (WSTRING) and `wc` (WCHAR). Before, these tags could not be parsed.
- **`lw` (LWORD) works.** Before, every LWORD access failed.
- **LINT and LWORD arrays return all 8 bytes of each element.**
- **WSTRING (`PlcEncoding.Unicode` strings) reads and writes work.**
  - Reads return only the actual characters.
  - Writes set the maximum length correctly.
  - `ReadItem.NumberOfItems` of a WSTRING now counts its 4 header bytes as 2 items of 2 bytes, instead of 4 items.
- **`WriteItem.CreateChild` takes the length in items, as documented.** It sliced the data in bytes before.
- **Messages larger than the negotiated TPDU size are sent in several fragments, and fragmented messages are reassembled.** Before, only the first fragment was sent, and reassembling threw an exception.
- **A connection that breaks while a fragmented message is received no longer blocks the client from reconnecting.** Before, every later connect of that client instance failed.
- **Alarm updates are registered again after a reconnect.** Before, the client kept the registration of the old connection, and subscriptions waited forever.

### Server (`Dacs7Server`)

- **The negotiated PDU size and number of parallel jobs of the communication setup are used.** Before, the values the client requested were used.
- **Read and write jobs which do not fit into the negotiated PDU are rejected like a PLC does**, with an ack with error class 0x85. Before, they were answered.

## 2.2.7

Bugfix release. No public API was removed or renamed, and the client's behaviour is unchanged except for the bugs fixed below.

**Strong name:** the assemblies are public signed with the public key of the original key pair. They keep the identity of the previous packages (`PublicKeyToken=3d20fc192b993b99`), so references compiled against older versions still bind. .NET Core and .NET 5+ do not check strong name signatures. .NET Framework applications running in full trust load the assemblies as well, because they skip the verification by default (strong name bypass). Public signed assemblies cannot be installed into the GAC, and they do not load if strong name bypass is disabled on the machine.

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
