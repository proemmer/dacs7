[![insite MyGet Build Status](https://www.myget.org/BuildSource/Badge/insite?identifier=1f729347-9ff3-4bb4-bcad-5be664a807c9)](https://www.myget.org/)

# dacs7

Data access S7 is a library to connect to S7 plcs for reading and writing data.

<!-- TOC -->

- [dacs7](#dacs7)
- [NuGet](#nuget)
- [Description](#description)
- [Sample-Code](#sample-code)
    - [Open and close a connection to the PLC](#open-and-close-a-connection-to-the-plc)
    - [Using tag methods](#using-tag-methods)
        - [Read and Write byte data](#read-and-write-byte-data)
        - [Read and Write bit data](#read-and-write-bit-data)
    - [Using ItemSpecification methods](#using-itemspecification-methods)
        - [Read and Write byte data](#write-and-read-single-data)
- [Compatibility](#compatibility)
    - [Additional TIA Settings (1200 and 1500 CPUs)](#additional-tia-settings-1200-and-1500-cpus)
        - [DB Properties](#db-properties)
        - [FullAccess](#fullaccess)
        - [Connection mechanisms](#connection-mechanisms)

<!-- /TOC -->


# NuGet
    PM>  Install-Package Dacs7

# Description


Dacs7 is used to connect to a SIEMENS Plc by using the RFC1006 protocol to perform operations.

# Sample-Code


## Open and close a connection to the PLC
```cs

//create an instance of the client
var client = new Dacs7Client("127.0.0.1:102,0,2");

//connect to the plc. If the connection could not be established
//you will get an exception here.
await client.ConnectAsync();

//Check if the client is connected. If yes, than close the connection.
if(client.IsConnected)
    await client.DisconnectAsync();
```

## Using tag methods

This kind of methods are the low level methods for communicate with the PLC.

### Read and Write byte data
```cs
var testData1 = new byte[100];
var testData2 = new byte[500];

//Write an array of bytes to the PLC. Syntax = Area.Offset,DataType[,length]
await _client.WriteAsync(KeyValuePair.Create<string, object>("DB1114.0,b,100", testData1), KeyValuePair.Create<string, object>("DB1114.100,b,500", testData2));

//Read an array of bytes from the PLC. Syntax = Area.Offset,DataType[,length]
var readResult = await _client.ReadAsync("DB1114.0,b,100", "DB1114.100,b,500");

```

### Read and Write bit data
```cs

// TODO

```


## Using ItemSpecification methods

This kind of functions are the higher level methods. By using this you write and read the data direct by using
the .Net type system.

```cs
var testData1 = new byte[100];
var testData2 = new byte[500];

//Write an array of bytes to the PLC. 
await _client.WriteAsync(WriteItem.Create<byte[]>("DB1114", 0, testData1), WriteItem.Create<byte[]>("DB1114", 100, testData2));

//Read an array of bytes from the PLC. 
var readResult = await _client.ReadAsync(ReadItem.Create<byte[]>("DB1114", 0, 100), ReadItem.Create<byte[]>"DB1114", 100, 500);

```


# Compatibility

|             | 300 | 400 | WinAC | 1200 | 1500 |
|:------------|:---:|:---:|:-----:|:----:|:----:|
|DB Read/Write|  X  |  X  |   X   |   X  |   X  |
|EB Read/Write|  X  |  X  |   X   |   X  |   X  |
|AB Read/Write|  X  |  X  |   X   |   X  |   X  |
|MB Read/Write|  X  |  X  |   X   |   X  |   X  |
|TM Read/Write|  X  |  X  |   X   |      |      |
|CT Read/Write|  X  |  X  |   X   |      |      |

## Additional TIA Settings (1200 and 1500 CPUs)

### DB Properties

Select the DB in the left pane under 'Program blocks' and click 'Properties' in the context menu.

<image src="images/BlockSettings.PNG"/>

### FullAccess

Select the CPU in the left pane and click 'Properties' in the context menu and go to 'Protection & Security'.

<image src="images/FullAccess.PNG"/>

### Connection mechanisms

Select the CPU in the left pane and click 'Properties' in the context menu and go to 'Protection & Security/Connection mechanisms'.

<image src="images/Connectionmechanism.PNG"/>

