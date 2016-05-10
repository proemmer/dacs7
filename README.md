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
