using Dacs7.Domain;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dacs7
{
    public delegate void OnConnectionChangeEventHandler(object sender, PlcConnectionNotificationEventArgs e);

    public interface IDacs7Client
    {
        UInt16 ReadItemMaxLength { get; }
        UInt16 WriteItemMaxLength { get; }

        /// <summary>
        /// Setup the connection.
        /// </summary>
        /// <returns></returns>
        string ConnectionString { get; set; }

        /// <summary>
        /// True if client is connected to plc
        /// </summary>
        /// <returns></returns>
        bool IsConnected { get; }

        /// <summary>
        /// Get the determined PDU Size
        /// </summary>
        UInt16 PduSize { get; }

        /// <summary>
        /// Connect to the plc.
        /// </summary>
        /// <param name="connectionString">"Data Source=192.168.0.145,102,0,2" -> IP,Port,Rack,Slot</param>
        void Connect(string connectionString = null);


        /// <summary>
        /// Connect to the plc asynchronous.
        /// </summary>
        /// <param name="connectionString">"Data Source=192.168.0.145,102,0,2" -> IP,Port,Rack,Slot</param>
        Task ConnectAsync(string connectionString = null);

        /// <summary>
        /// Disconnect from plc
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Disconnect from plc asynchronous.
        /// </summary>
        /// <returns></returns>
        Task DisconnectAsync();

        /// <summary>
        /// This event is call if the connection to plc was changed
        /// </summary>
        event OnConnectionChangeEventHandler OnConnectionChange;

        /// <summary>
        /// Read data from the given data block number at the given offset.
        /// The length of the data will be extracted from the generic parameter.
        /// </summary>
        /// <typeparam name="T">Type of the return value</typeparam>
        /// <param name="dbNumber">This parameter specifies the number of the data block in the PLC</param>
        /// <param name="offset">This is the offset to the data you want to read.(offset is normally the number of bytes from the beginning of the area, 
        /// excepted the data type is a boolean, then the offset is in number of bits. This means ByteOffset*8+BitNumber)</param>
        /// <returns>The read value or the default value of the type if the data could not be read.</returns>
        T ReadAny<T>(int dbNumber, int offset);

        /// <summary>
        /// Read data async from the given data block number at the given offset.
        /// The length of the data will be extracted from the generic parameter.
        /// </summary>
        /// <typeparam name="T">Type of the return value</typeparam>
        /// <param name="dbNumber">This parameter specifies the number of the data block in the PLC</param>
        /// <param name="offset">This is the offset to the data you want to read.(offset is normally the number of bytes from the beginning of the area, 
        /// excepted the data type is a boolean, then the offset is in number of bits. This means ByteOffset*8+BitNumber)</param>
        /// <returns>The read value or the default value of the type if the data could not be read.</returns>
        Task<T> ReadAnyAsync<T>(int dbNumber, int offset);


        /// <summary>
        /// Read a number of items from the given generic type form the given data block number at the given offset and try convert it to the given dataType.
        /// </summary>
        /// <typeparam name="TElement">Element type of the resulting enumerable</typeparam>
        /// <param name="dbNumber">This parameter specifies the number of the data block in the PLC</param>
        /// <param name="offset">This is the offset to the data you want to read.(offset is normally the number of bytes from the beginning of the area, 
        /// excepted the data type is a boolean, then the offset is in number of bits. This means ByteOffset*8+BitNumber)</param>
        /// <param name="numberOfItems">Number of items of the T to read. This could be the string length for a string, or the number of bytes/int for an array and so on. 
        /// The default value is always 1.</param>
        /// <returns>A list of TElement</returns>
        IEnumerable<T> ReadAny<T>(int dbNumber, int offset, int numberOfItems);

        /// <summary>
        /// Read a number of items async from the given generic type form the given data block number at the given offset and try convert it to the given dataType.
        /// </summary>
        /// <typeparam name="TElement">Element type of the resulting enumerable</typeparam>
        /// <param name="dbNumber">This parameter specifies the number of the data block in the PLC</param>
        /// <param name="offset">This is the offset to the data you want to read.(offset is normally the number of bytes from the beginning of the area, 
        /// excepted the data type is a boolean, then the offset is in number of bits. This means ByteOffset*8+BitNumber)</param>
        /// <param name="numberOfItems">Number of items of the T to read. This could be the string length for a string, or the number of bytes/int for an array and so on. 
        /// The default value is always 1.</param>
        /// <returns>A list of TElement</returns>
        Task<IEnumerable<T>> ReadAnyAsync<T>(int dbNumber, int offset, int numberOfItems);

        /// <summary>
        /// Read data from the PLC and return them as an array of byte.
        /// </summary>
        /// <param name="area">The target <see cref="PlcArea"></see> from which we want to read.</param>
        /// <param name="offset">This is the offset to the data you want to read.(offset is normally the number of bytes from the beginning of the area, 
        /// excepted the data type is a boolean, then the offset is in number of bits. This means ByteOffset*8+BitNumber)</param>
        /// <param name="type">Specify the .Net data type for the read data, to determine the data size to read</param>
        /// <param name="args">Arguments depending on the area. First argument is the number of items multiplied by the size of an item to read. If area is DB, second parameter is the db number.
        /// For example if you will read 500 bytes,  then you have to pass type = typeof(byte) and as first arg you have to pass 500.</param>
        /// <returns>The read <see cref="T:byte[]"/></returns>
        byte[] ReadAny(PlcArea area, int offset, Type type, params int[] args);

        /// <summary>
        /// Read multiple variables with one call from the PLC and return them as a list of the correct read types.
        /// </summary>
        /// <param name="parameters">A list of <see cref="ReadOperationParameter"/>, so multiple read requests can be handled in one message</param>
        /// <returns>A list of <see cref="T:object"/> where every list entry contains the read value in order of the given parameter order</returns>
        IEnumerable<byte[]> ReadAnyRaw(IEnumerable<ReadOperationParameter> parameters);

        /// <summary>
        /// Read multiple variables with one call from the PLC and return them as a list of the correct read types.
        /// </summary>
        /// <param name="parameters">A list of <see cref="ReadOperationParameter"/>, so multiple read requests can be handled in one message</param>
        /// <returns>A list of <see cref="T:object"/> where every list entry contains the read value in order of the given parameter order</returns>
        IEnumerable<object> ReadAny(IEnumerable<ReadOperationParameter> parameters);

        /// <summary>
        /// Read data from the PLC as parallel. The size of a message to and from the PLC is limited by the PDU-Size. This method splits the message
        /// and recombine it after receiving them. And this is done in parallel.
        /// </summary>
        /// <param name="area">The target <see cref="PlcArea"></see> from which we want to read.</param>
        /// <param name="offset">This is the offset to the data you want to read.(offset is normally the number of bytes from the beginning of the area, 
        /// excepted the data type is a boolean, then the offset is in number of bits. This means ByteOffset*8+BitNumber)</param>
        /// <param name="type">Specify the .Net data type for the read data. This parameter is used to determine the correct data length we have to read</param>
        /// <param name="args">Arguments depending on the area. First argument is the number of items multiplied by the size of an item to read. If area is DB, second parameter is the db number.
        /// For example if you will read 500 bytes,  then you have to pass type = typeof(byte) and as first arg you have to pass 500.</param>
        /// <returns>read <see cref="T:byte[]"/></returns>
        object ReadAnyParallel(PlcArea area, int offset, Type type, params int[] args);

        /// <summary>
        /// Read data from the PLC asynchronous and convert it to the given .Net type.
        /// This method wraps the call in a task.
        /// </summary>
        /// <param name="area">The target <see cref="PlcArea"></see> from which we want to read.</param>
        /// <param name="offset">This is the offset to the data you want to read.(offset is normally the number of bytes from the beginning of the area, 
        /// excepted the data type is a boolean, then the offset is in number of bits. This means ByteOffset*8+BitNumber)</param>
        /// <param name="type">Specify the .Net data type for the read data</param>
        /// <param name="args">Arguments depending on the area. First argument is the number of items multiplied by the size of an item to read. If area is DB, second parameter is the db number.
        /// For example if you will read 500 bytes,  then you have to pass type = typeof(byte) and as first arg you have to pass 500.</param>
        /// <returns></returns>
        Task<byte[]> ReadAnyAsync(PlcArea area, int offset, Type type, params int[] args);

        /// <summary>
        /// Write data to the given PLC area.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="area">The target <see cref="PlcArea"></see> we want to write.</param>
        /// <param name="offset">This is the offset to the data you want to write.(offset is normally the number of bytes from the beginning of the area, 
        /// excepted the data type is a boolean, then the offset is in number of bits. This means ByteOffset*8+BitNumber)</param>
        /// <param name="value">Should be the value you want to write.</param>
        /// <param name="length">the length of data in bytes you want to write  e.g. if you have a value byte[500] and you want to write
        /// only the first 100 bytes, you have to set length to 100. If length is not set, the correct size will be determined by the value size.</param>
        void WriteAny<T>(PlcArea area, int offset, T value, int length = -1);

        /// <summary>
        /// Write data to the given PLC data block with offset and length.
        /// </summary>
        /// <param name="dbNumber">This parameter specifies the number of the data block in the PLC</param>
        /// <param name="offset">This is the offset to the data you want to write.(offset is normally the number of bytes from the beginning of the area, 
        /// excepted the data type is a boolean, then the offset is in number of bits. This means ByteOffset*8+BitNumber)</param>
        /// <param name="value">Should be the value you want to write.</param>
        /// <param name="length">the length of data in bytes you want to write  e.g. if you have a value byte[500] and you want to write
        /// only the first 100 bytes, you have to set length to 100. If length is not set, the correct size will be determined by the value size.</param>
        void WriteAny<T>(int dbNumber, int offset, T value, int length = -1);

        /// <summary>
        /// Write data to the plc.
        /// </summary>
        /// <param name="area">Specify the area of the plc, Input, Output, ....</param>
        /// <param name="offset">Offset in the area</param>
        /// <param name="value">value write</param>
        /// <param name="args">arguments e.g. array length</param>
        void WriteAny(PlcArea area, int offset, object value, params int[] args);

        /// <summary>
        /// Write data parallel to the given PLC area.The size of a message to and from the PLC is limited by the PDU-Size. This method splits the message
        /// and recombine it after receiving them. And this is done in parallel.
        /// </summary>
        /// <param name="area">The target <see cref="PlcArea"></see> we want to write.</param>
        /// <param name="offset">This is the offset to the data you want to write.(offset is normally the number of bytes from the beginning of the area, 
        /// excepted the data type is a boolean, then the offset is in number of bits. This means ByteOffset*8+BitNumber)</param>
        /// <param name="value">Should be the value you want to write.</param>
        /// <param name="args">Arguments depending on the area. First argument is the number of items multiplied by the size of an item to write. If area is DB, second parameter is the db number.
        /// For example if you will write 500 bytes, then you have to pass type = typeof(byte) and as first arg you have to pass 500.</param>
        /// <returns></returns>
        void WriteAnyParallel(PlcArea area, int offset, object value, params int[] args);

        /// <summary>
        /// Write data to the plc asynchronous.
        /// </summary>
        /// <param name="area">Specify the area of the plc, Input, Output, ....</param>
        /// <param name="offset">Offset in the area</param>
        /// <param name="value">value write</param>
        /// <param name="args">arguments e.g. array length</param>
        Task WriteAnyAsync(PlcArea area, int offset, object value, params int[] args);

        /// <summary>
        /// Write multiple variables with one call to the PLC.
        /// </summary>
        /// <param name="parameters">A list of <see cref="WriteOperationParameter"/>, so multiple write requests can be handled in one message</param>
        void WriteAny(IEnumerable<WriteOperationParameter> parameters);

        /// <summary>
        /// Read the number of blocks in the PLC per type
        /// </summary>
        /// <returns><see cref="IPlcBlocksCount"/> where you have access to the count of all the block types.</returns>
        IPlcBlocksCount GetBlocksCount();

        /// <summary>
        /// Read the number of blocks in the PLC per type asynchronous. This means the call is wrapped in a Task.
        /// </summary>
        /// <returns><see cref="IPlcBlocksCount"/> where you have access to the count of all the block types.</returns>
        Task<IPlcBlocksCount> GetBlocksCountAsync();

        /// <summary>
        /// Get all blocks of the specified type.
        /// </summary>
        /// <param name="type">Block type to read. <see cref="PlcBlockType"/></param>
        /// <returns>Return a list off all blocks <see cref="IPlcBlock"/> of this type</returns>
        IEnumerable<IPlcBlocks> GetBlocksOfType(PlcBlockType type);

        /// <summary>
        /// Get all blocks of the specified type asynchronous.This means the call is wrapped in a Task.
        /// </summary>
        /// <param name="type">Block type to read. <see cref="PlcBlockType"/></param>
        /// <returns>Return a list off all blocks <see cref="IPlcBlock"/> of this type</returns>
        Task<IEnumerable<IPlcBlocks>> GetBlocksOfTypeAsync(PlcBlockType type);

        /// <summary>
        /// Read Meta information of a plc block.
        /// </summary>
        /// <param name="blockType">Type of the block e.g. Db</param>
        /// <param name="blocknumber">Number of the block</param>
        /// <returns></returns>
        IPlcBlockInfo ReadBlockInfo(PlcBlockType blockType, int blocknumber);

        /// <summary>
        /// Read the full data of a block from the plc.
        /// </summary>
        /// <param name="blockType">Specify the block type to read. e.g. DB</param>
        /// <param name="blocknumber">Specify the Number of the block</param>
        /// <returns></returns>
        byte[] UploadPlcBlock(PlcBlockType blockType, int blocknumber);


        ///// <summary>
        ///// Write the full data of a block to the plc.
        ///// </summary>
        ///// <param name="blockType">Specify the block type to read. e.g. DB</param>
        ///// <param name="blocknumber">Specify the Number of the block</param>
        ///// <param name="data">Plc block in byte</param>
        ///// <returns></returns>
        //bool DownloadPlcBlock(PlcBlockType blockType, int blocknumber, byte[] data);

        /// <summary>
        /// Read Meta information of a plc block asynchronous.
        /// </summary>
        /// <param name="blockType">Type of the block e.g. Db</param>
        /// <param name="blocknumber">Number of the block</param>
        /// <returns></returns>
        Task<IPlcBlockInfo> ReadBlockInfoAsync(PlcBlockType blockType, int blocknumber);

        /// <summary>
        /// Return a list of pending alarms
        /// </summary>
        /// <returns></returns>
        IEnumerable<IPlcAlarm> ReadPendingAlarms();

        /// <summary>
        /// Return a list of pending alarms
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<IPlcAlarm>> ReadPendingAlarmsAsync();
    }
}
