# dacs7

Data access S7 is a library to connect to S7 plcs for reading and writing data.

NuGet
=====
PM>  Install-Package Dacs7 -Pre 

Description
==============================

Dacs7 is used to connect to a SIEMENS Plc by usin the RFC1006 connection an perform operations.

Sample-Code
==============================

The following code should show you the fundamental usage:

<pre><code>
var connectionString = "Data Source=128.0.0.1:102,0,2";
var client = new Dacs7Client();

client.Connect(connectionString);

var length = 500;
var testData = new byte[length];
var offset = 0;
var dbNumber = 560;

_client.WriteAny(PlcArea.DB, offset, testData, new[] { length, dbNumber });

var red = _client.ReadAny(PlcArea.DB, offset, typeof(byte), new[] { length, dbNumber }) as byte[];

client.Disconnect();
</code></pre>


The following code should show you the usage with generic methods:

<pre><code>
        public static void GenericsSample()
        {
            var boolValue = _client.ReadAny<bool>(TestDbNr, TestBitOffset);
            var intValue = _client.ReadAny<int>(TestDbNr, TestByteOffset);

            const int numberOfArrayElements = 2;
            var boolEnumValue = _client.ReadAny<bool>(TestDbNr, TestBitOffset, numberOfArrayElements);
            var intEnumValue = _client.ReadAny<int>(TestDbNr, TestByteOffset, numberOfArrayElements);
        }
</code></pre>

Read/Write multiple variables in one call:

<pre><code>
        public static void MultiValuesSample()
        {
            var operations = new List<ReadOperationParameter>
            {
                new ReadOperationParameter{Area = PlcArea.DB, Offset= TestByteOffset, Type=typeof(byte), Args = new int[]{1, TestDbNr}},
                new ReadOperationParameter{Area = PlcArea.DB, Offset= TestBitOffset, Type=typeof(bool), Args = new int[]{1, TestDbNr}}
            };

            var result = _client.ReadAny(operations); //result is IEnumerable<byte[]>

            var writeOperations = new List<WriteOperationParameter>
            {
                new WriteOperationParameter{Area = PlcArea.DB, Offset= TestByteOffset, Type=typeof(byte), Args = new int[]{1, TestDbNr}, Data = (byte)0x05},
                new WriteOperationParameter{Area = PlcArea.DB, Offset= TestBitOffset, Type=typeof(bool), Args = new int[]{1, TestDbNr}, Data = true}
            };

            _client.WriteAny(writeOperations);

        }
</code></pre>


Release Notes
==============================
* 1.0.4:  -change ReadAny results from object to byte[] and add some generic methods for ReadAny.
          -implement ReadAny and Write any for multible variables in one call job. (for now there is no automatic splitting        implemented if the size of the pdu is to large. To large pdu's result in an Dacs7ToMuchDataPerCallException )
