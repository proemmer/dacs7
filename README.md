[![insite MyGet Build Status](https://www.myget.org/BuildSource/Badge/insite?identifier=1f729347-9ff3-4bb4-bcad-5be664a807c9)](https://www.myget.org/)

# dacs7

Data access S7 is a library to connect to S7 plcs for reading and writing data.

<!-- TOC -->

- [dacs7](#dacs7)
- [NuGet](#nuget)
- [Description](#description)
- [Sample-Code](#sample-code)
    - [Open and close a connection to the PLC](#open-and-close-a-connection-to-the-plc)
    - [Using non generic methods](#using-non-generic-methods)
        - [Read and Write byte data](#read-and-write-byte-data)
        - [Read and Write bit data](#read-and-write-bit-data)
    - [Using generic methods](#using-generic-methods)
        - [Write and read single data](#write-and-read-single-data)
        - [Write and read multiple data as a list](#write-and-read-multiple-data-as-a-list)
    - [Multi operation methods:](#multi-operation-methods)
        - [Read/Write multiple variables in one call:](#readwrite-multiple-variables-in-one-call)
        - [Read/Write multiple variables by using the create methods:](#readwrite-multiple-variables-by-using-the-create-methods)
- [Compatibility](#compatibility)
    - [Additional TIA Settings (1200 and 1500 CPUs)](#additional-tia-settings-1200-and-1500-cpus)
        - [DB Properties](#db-properties)
        - [FullAccess](#fullaccess)
        - [Connection mechanisms](#connection-mechanisms)
- [Release Notes](#release-notes)

<!-- /TOC -->


# NuGet
    PM>  Install-Package Dacs7

# Description


Dacs7 is used to connect to a SIEMENS Plc by using the RFC1006 protocol to perform operations.

# Sample-Code


## Open and close a connection to the PLC
```cs
var connectionString = "Data Source=127.0.0.1:102,0,2";

//create an instance of the client
var client = new Dacs7Client();

//connect to the plc. If the connection could not be established
//you will get an exception here.
client.Connect(connectionString);

//Check if the client is connected. If yes, than close the connection whit a call 
//of disconnect.
if(client.IsConnected)
    client.Disconnect();
```

## Using non generic methods

This kind of methods are the low level methods for communicate with the PLC.

### Read and Write byte data
```cs
var length = 500;
var testData = new byte[length];
var offset = 10; //The offset is the number of bytes from the beginning of the area
var dbNumber = 560;

//Write an array of bytes to the PLC. 
_client.WriteAny(PlcArea.DB, offset, testData, new[] { length, dbNumber });

//Read an array of bytes from the PLC.
var readResult = _client.ReadAny(PlcArea.DB, offset, typeof(byte), new[] { length, dbNumber });

```

### Read and Write bit data
```cs
var length = 1;
var testData = true;
var offset = 10; //For bitoperations we need to specify the offset in bits  (byteoffset * 8 + bitnumber)
var dbNumber = 560;

//Write a bit to the PLC.
client.WriteAny(PlcArea.DB, offset, testData, new int[] { length, dbNumber });

//Read a bit from the PLC
var state = client.ReadAny(PlcArea.DB, offset*8, typeof(bool), new int[] { length, dbNumber });

```


## Using generic methods

This kind of functions are the higher level methods. By using this you write and read the data direct by using
the .Net type system.

### Write and read single data
```cs
//Write and read bool values. Attention: Use bit offset, because we address bits.
client.WriteAny<bool>(TestDbNr, TestBitOffset, true);
var boolRes = client.ReadAny<bool>(TestDbNr, TestBitOffset);

//Write and read short (INT in PLC) values. Attention: Use byte offset, because we address non bits.
client.WriteAny(TestDbNr, TestByteOffset, (short)1);
var shortRes = client.ReadAny<short>(TestDbNr, TestByteOffset);

//Write and read string values. Attention: Use byte offset, because we address non bits.
client.WriteAny(TestDbNr, TestByteOffset, "TEST");
var stringRes = client.ReadAny<string>(TestDbNr, TestByteOffset,4);

//Write and read char values. Attention: Use byte offset, because we address non bits.
client.WriteAny(TestDbNr, TestByteOffset, "TEST".ToArray());
var charRes = client.ReadAny<char>(TestDbNr, TestByteOffset, 4);
```

### Write and read multiple data as a list
```cs
//Write and read a list of bool values. Attention: Use bit offset, because we address bits.
//In this case you can only read values without a gap.
var writeVal = new bool[] { true, true };
client.WriteAny(TestDbNr, TestBitOffset, writeVal);
var boolarrayRes = client.ReadAny<bool>(TestDbNr, TestBitOffset, writeVal.Length);
```

## Multi operation methods:

This methods can handle multiple read or write operations in a single message. This will increase the performance by reducing the communication effort, 
because you have only on message for all operations instead of one message for each operation.

Attention: Use this methods only for a small amount of data, because it does not support the auto split mechanism. This means, if the data are to long
for a message, the message will not be split into several messages automatically (you will get an exception). The size of one message is dependent by the PLC you are using. 
Normally you can address up to 900 bytes in one message.


### Read/Write multiple variables in one call:

```cs
public static void MultiValuesSample()
{
    var operations = new List<ReadOperationParameter>
    {
        new ReadOperationParameter{Area = PlcArea.DB, Offset= TestByteOffset, 
                                   Type=typeof(byte), Args = new int[]{1, TestDbNr}},
        new ReadOperationParameter{Area = PlcArea.DB, Offset= TestBitOffset, 
                                  Type=typeof(bool), Args = new int[]{1, TestDbNr}}
    };

    var result = _client.ReadAny(operations); //result is IEnumerable<byte[]>

    var writeOperations = new List<WriteOperationParameter>
    {
        new WriteOperationParameter{Area = PlcArea.DB, Offset= TestByteOffset, 
                                    Type=typeof(byte), Args = new int[]{1, TestDbNr}, Data = (byte)0x05},
        new WriteOperationParameter{Area = PlcArea.DB, Offset= TestBitOffset, 
                                    Type=typeof(bool), Args = new int[]{1, TestDbNr}, Data = true}
    };

    _client.WriteAny(writeOperations);

}
```


### Read/Write multiple variables by using the create methods:

```cs

client.WriteAny(new List<WriteOperationParameter>
{
    //Create a write operation to write a byte to the byte offset
    WriteOperationParameter.Create(TestDbNr,TestByteOffset,(byte)0x00),

    //Create a write operation to write a bit to the bit offset (byteoffset*8+bitnumber))
    WriteOperationParameter.Create(TestDbNr,TestBitOffset,false),

    //Create a write operation to write a bit by using the bit creator method.
    //Here you can use the byte offset and then you specify the bitnumber.
    WriteOperationParameter.CreateForBit(TestDbNr,TestByteOffset,0, false),

    //Create a write operation to write a string to the byte offset
    WriteOperationParameter.Create(TestDbNr,TestByteOffset+100,"    "),

    //Create a write operation to write a short (PLC INT) to the byte offset
    WriteOperationParameter.Create(TestDbNr,TestByteOffset+110,(short)0)
});

result = client.ReadAny(new List<ReadOperationParameter>
{
    //Create a read operation to read a byte from the byte offset
    ReadOperationParameter.Create<byte>(TestDbNr,TestByteOffset),

    //Create a read operation to read a bit from the bit offset (byteoffset*8+bitnumber))
    ReadOperationParameter.Create<bool>(TestDbNr,TestBitOffset),

    //Create a read operation to read a bit by using the bit creator method.
    //Here you can use the byte offset and then you specify the bitnumber.
    ReadOperationParameter.CreateForBit(TestDbNr,TestByteOffset,0),

    //Create a read operation to read a string from the byte offset
    ReadOperationParameter.Create<string>(TestDbNr,TestByteOffset+100, 4),

    //Create a read operation to read a short (PLC INT) from the byte offset
    ReadOperationParameter.Create<short>(TestDbNr,TestByteOffset+110)
}).ToArray();

Assert.AreEqual((byte)0x05, result[0]);
Assert.AreEqual(true, result[1]);
Assert.AreEqual("TEST", result[2]);

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

###DB Properties

Select the DB in the left pane under 'Program blocks' and click 'Properties' in the context menu.

<image src="images/BlockSettings.PNG"/>

### FullAccess

Select the CPU in the left pane and click 'Properties' in the context menu and go to 'Protection & Security'.

<image src="images/FullAccess.PNG"/>

### Connection mechanisms

Select the CPU in the left pane and click 'Properties' in the context menu and go to 'Protection & Security/Connection mechanisms'.

<image src="images/Connectionmechanism.PNG"/>




# Release Notes
* 1.0.6:
    * migrated to VS2017 and C#7.
    * Fixed a bug in auto reconnect.
    * Refactoring
* 1.0.5:
    * fix a bug in generic read methods when reading bits.
* 1.0.4:  
    * change ReadAny results from object to byte[] and add some generic methods for ReadAny.
    * implement ReadAny and Write any for multible variables in one call job. (for now there is no automatic splitting        
     implemented if the size of the pdu is to large. To large pdu's result in an Dacs7ToMuchDataPerCallException )
