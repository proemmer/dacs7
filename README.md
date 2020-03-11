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

//create an instance of the client   [IP]:[Port],[Rack],[Slot]
var client = new Dacs7Client("127.0.0.1:102,0,2");

//connect to the plc. If the connection could not be established
//you will get an Dacs7NotConnectedException here.
await client.ConnectAsync();

...

//Check if the client is connected. If yes, than close the connection.
if(client.IsConnected)
    await client.DisconnectAsync();
```

## Using tag methods

This methods are created to address data a given a tag string. All the relevant information will be parsed from this string.

*Syntax*

`Area.Offset,DataType[,Length]`


* **Areas**:  dacs7 supports english and german mnemonic and also its own syntax (The input is not case sensitive)
    * **Inputs**:       i, e, ib
    * **Marker**:       m, f
    * **Outputs**:      q, a, qb
    * **Timer**:        t, tm
    * **Counter**:      c, z, ct
    * **Data Blocks**:  DB[number]  
* **Offset**:  The offset in byte from the beginning of the specified area.
* **DataType**:
    * **x[bit]**:  Boolean  using a bit you also have to specify the bit number in the datatype part.
    * **b**:       Byte
    * **w**:       Word (ushort)
    * **dw**:      DWord (uint)
    * **lw**:      LongWord (ulong) 
    * **si**:      SmallInt (sbyte)
    * **i**:       Int (short)
    * **di**:      DInt (int)
    * **li**:      LongInt (long) 
    * **r**:       Real (float)
    * **c**:       Char
    * **wc**:      WChar  (string)
    * **s**:       String
    * **ws**:      WString (string)
* **Length**: This part is optional, the default value is 1. (a special case is the string type, this specifies the length of the string, so it is currently not possible to read a string array in one command);

### Write Results

For each write operation you will get an ItemResponseRetValue.

```cs
    public enum ItemResponseRetValue : byte
    {
        Reserved = 0x00,
        [Description("Hardware error")]
        HardwareFault = 0x01,

        [Description("Accessing the object not allowed")]
        AccessFault = 0x03,

        [Description("Invalid address")]
        OutOfRange = 0x05,       //the desired address is beyond limit for this PLC 

        [Description("Data type not supported")]
        NotSupported = 0x06,     //Type is not supported 

        [Description("Data type inconsistent")]
        SizeMismatch = 0x07,     //Data type inconsistent 

        [Description("Object does not exist")]
        DataError = 0x0a,        //the desired item is not available in the PLC, e.g. when trying to read a non existing DB

        [Description("Success")]
        Success = 0xFF,
    }
```

### Read Results

For each read operation you will get and DataValue.

```cs
    public class DataValue
    {
        ItemResponseRetValue ReturnCode { get; }
        bool IsSuccessReturnCode { get; }
        Type Type { get; }
        Memory<byte> Data { get; }
        object Value { get; };
        T GetValue<T>()

    }
```



### Read and Write byte data
```cs
var testData1 = new byte[100];
var testData2 = new byte[500];

//Write an array of bytes to the PLC by using the tag syntax
var writeResult1 = await _client.WriteAsync(WriteItem.CreateFromTag("DB1114.0,b,100", testData1), 
                                           WriteItem.CreateFromTag("DB1114.100,b,500", testData2));

//Read an array of bytes from the PLC by using the tag syntax
var readResults1 = await _client.ReadAsync(ReadItem.CreateFromTag("DB1114.0,b,100"), 
                                          ReadItem.CreateFromTag("DB1114.100,b,500"));

//Write an array of bytes to the PLC 
var writeResult2 = await _client.WriteAsync(WriteItem.Create("DB1114",0, testData1), 
                                           WriteItem.Create("DB1114",100, testData2));

//Read an array of bytes from the PLC 
var readResults2 = await _client.ReadAsync(ReadItem.Create<byte[]>("DB1114", 0, 100), 
                                          ReadItem.Create<byte[]>("DB1114", 100, 500));

```

### Read and Write bit data

The offset is normally in bytes, but if you address bools, you have to pass the address in bits (byteoffset * 8 + bitoffset)

```cs
var readResults = await client.ReadAsync(ReadItem.Create<bool>(datablock, baseOffset),
                                         ReadItem.Create<bool>(datablock, baseOffset + 5))

await client.WriteAsync(WriteItem.Create(datablock, baseOffset, true),
                        WriteItem.Create(datablock, baseOffset + 5, true))

```


### Read and Write string data

IF the given type is a string or char you can also specify if its the Unicode variant of them (this means 2byte per sign).
PlcEncoding can be Acsii or Unicode.
Unicode is only supported in TIA to address WString an WChar.

```cs
var readResults = await client.ReadAsync(ReadItem.Create<string>(datablock, 0, 10, PlcEncoding.Ascii))

await client.WriteAsync(WriteItem.Create(datablock, baseOffset, "TEST      ", PlcEncoding.Ascii))

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

