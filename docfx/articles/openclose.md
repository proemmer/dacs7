## Open and close a connection to the PLC

Creating an instance of the dacs7 client has a couple of optional settings. The minimum you have to specify is the IP address.
This means you can connect to a plc by using only `127.0.0.1`. The syntax for the address parameter is

` IP:[Port],[Rack],[Slot]`

where each parameter except the IP address is optional, but if you like to specify the rack, you also have to specify the port.
The default parameters for the optional parameters are:

* Port: 102
* Rack: 0
* slot: 2

```cs
//create an instance of the client
var client = new Dacs7Client("127.0.0.1:102,0,2");
```

A more advanced constructor which can be used is the following one where you can specify some additional options.

```cs

var client = new Dacs7Client( address: "127.0.0.1:102,0,2", 
                              connectionType: PlcConnectionType.Pg, 
                              timeout: 5000, 
                              loggerFactory = null, 
                              autoReconnectTime: 5000);
```

Parameters for connection type:

```cs
    public enum PlcConnectionType : ushort
    {
        Pg = 0x01,
        Op = 0x02,
        Basic = 0x03
    }
```

The timeout is used for read and write operations to wait for a response. In the case of the connect operation, dacs7 use 2 * timeout.


Auto reconnect time is the time to wait before dacs7 tries to reconnect the socket.



After an instance was created you can use `ConnectAsync` to connect to the plc and `DisconnectAsync` to disconnect. If the connection could not be established, dacs7 throws a  `Dacs7NotConnectedException`.




```cs
await client.ConnectAsync();
...
await client.DisconnectAsync();
```