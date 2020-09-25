# read from plc

You have a couple of possibilities to read data from the plc. These are described below.

You can read more the one data in a single call of the read method by providing the read items to the method.

For each read item you will get a DataValue as a result.

```cs
    public class DataValue
    {
        ItemResponseRetValue ReturnCode { get; }
        bool IsSuccessReturnCode { get; }
        Type Type { get; }
        Memory<byte> Data { get; }
        object Value { get; };
        T GetValue<T>();

        string GetValueAsString(this DataValue dataValue, string separator = " ");
        string GetValueAsString(this DataValue dataValue, DataValueFormatter formatter, string separator = " ")
    }
```


## Read data by using the tag syntax

For details of this syntax see the area TagSyntax.


```cs

// Read arrays of bytes from the PLC by using the tag syntax shorthand method
var readResults1 = await _client.ReadAsync("DB1114.0,b,100", "DB1114.100,b,500");

// Read arrays of bytes from the PLC by using the tag syntax and the read items.
var readResults1 = await _client.ReadAsync(ReadItem.CreateFromTag("DB1114.0,b,100"), 
                                           ReadItem.CreateFromTag("DB1114.100,b,500"));


```


## Read data by using the read item class

```cs
var readResults = await _client.ReadAsync(ReadItem.Create<ushort>("DB1114", 0, 100), 
                                          ReadItem.Create<byte[]>("DB1114", 100, 500));
if (results.Count() == 2)
{
    if(readResults[0].IsSuccessReturnCode) 
    {
        Console.WriteLine(readResults[0].Value);
    }
    // or
    if(readResults[1].IsSuccessReturnCode) 
    {
        Console.WriteLine(readResults[1].GetValue<byte[]>());
    }
}
```


## Read and write bit data

The offset is normally in bytes, but if you address a Boolean, you have to pass the address in bits (byteOffset * 8 + bitOffset).

```cs
var readResults = await client.ReadAsync(ReadItem.Create<bool>("DB1", baseOffset),
                                         ReadItem.Create<bool>("DB1", baseOffset + 5))

```


### Read and Write string data

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
var readResults = await client.ReadAsync(ReadItem.Create<string>("DB1", 0, 10, PlcEncoding.Windows1252))

```


