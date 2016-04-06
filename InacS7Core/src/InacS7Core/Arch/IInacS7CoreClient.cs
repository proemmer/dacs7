using InacS7Core.Domain;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InacS7Core.Arch
{
    public interface IInacS7CoreClient
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
        /// Read data from the plc
        /// </summary>
        /// <param name="area">Specify the area of the plc, Input, Output, ....</param>
        /// <param name="offset">Offset in the area</param>
        /// <param name="type">data type to read</param>
        /// <param name="args">arguments e.g. array length</param>
        /// <returns></returns>
        object ReadAny(PlcArea area, int offset, Type type, params int[] args);

        /// <summary>
        /// Read data from the plc asynchronous.
        /// </summary>
        /// <param name="area">Specify the area of the plc, Input, Output, ....</param>
        /// <param name="offset">Offset in the area</param>
        /// <param name="type">data type to read</param>
        /// <param name="args">arguments e.g. array length</param>
        /// <returns></returns>
        Task<object> ReadAnyAsync(PlcArea area, int offset, Type type, params int[] args);

        /// <summary>
        /// Write data to the plc.
        /// </summary>
        /// <param name="area">Specify the area of the plc, Input, Output, ....</param>
        /// <param name="offset">Offset in the area</param>
        /// <param name="value">value write</param>
        /// <param name="args">arguments e.g. array length</param>
        void WriteAny(PlcArea area, int offset, object value, params int[] args);

        /// <summary>
        /// Write data to the plc asynchronous.
        /// </summary>
        /// <param name="area">Specify the area of the plc, Input, Output, ....</param>
        /// <param name="offset">Offset in the area</param>
        /// <param name="value">value write</param>
        /// <param name="args">arguments e.g. array length</param>
        Task WriteAnyAsync(PlcArea area, int offset, object value, params int[] args);

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


        /// <summary>
        /// Write the full data of a block to the plc.
        /// </summary>
        /// <param name="blockType">Specify the block type to read. e.g. DB</param>
        /// <param name="blocknumber">Specify the Number of the block</param>
        /// <param name="data">Plc block in byte</param>
        /// <returns></returns>
        bool DownloadPlcBlock(PlcBlockType blockType, int blocknumber, byte[] data);

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
