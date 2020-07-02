
# write to plc

You have a couple of possibilities to write data to the plc. These are described below.

You can write more the one data in a single call of the write method by providing the write items to the method.

For each write item you will get a ItemResponseRetValue as a result.


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



## Read data by using the tag syntax

For details of this syntax see the area TagSyntax.


```cs
var data1 = new byte[100];
var data2 = new byte[500];

// Write arrays of bytes to the PLC by using the tag syntax shorthand method
var writeResults1 = await _client.WriteAsync(("DB1114.0,b,100", data1), ("DB1114.100,b,500", data2));

// Write arrays of bytes from the PLC by using the tag syntax and the write items.
var readResults2 = await _client.WriteAsync(WriteItem.CreateFromTag("DB1114.0,b,100", data1), 
                                           WriteItem.CreateFromTag("DB1114.100,b,500", data2));


```


## Write data by using the read item class

```cs
var data = new byte[500];

var writeResults = await _client.WriteAsync(WriteItem.Create<ushort>("DB1114", 0, 100, (ushort)0x02), 
                                          WriteItem.Create<byte[]>("DB1114", 100, 500, data));
if (writeResults.Count() == 2)
{
    Console.WriteLine(writeResults[0]);
    Console.WriteLine(writeResults[1]);
}
```


## Write bit data

The offset is normally in bytes, but if you address a Boolean, you have to pass the address in bits (byteOffset * 8 + bitOffset).

```cs
var writeResults = await client.WriteAsync(WriteItem.Create<bool>("DB1", baseOffset, false),
                                         WriteItem.Create<bool>("DB1", baseOffset + 5, true))

```


### Write string data

If the given type is a string or char you can also specify if its the Unicode variant of them (this means 2byte per sign).
You also have to specify the encoding of the strings.

currently we support the following encodings:

```cs
    public enum PlcEncoding
    {
        UTF7,           // for normal strings 
        Unicode,        // for wide strings (used 2 bytes per letter)
        Windows1252     // default encoding for normal strings
    }
```

Unicode is only supported in TIA to address WString an WChar.



```cs
var writeResults = await client.WriteAsync(WriteItem.Create<string>("DB1", 0, 10, "Test", PlcEncoding.Windows1252))

```


