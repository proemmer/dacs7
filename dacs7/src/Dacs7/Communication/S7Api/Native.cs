using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Dacs7.Communication.S7Api
{
    internal class Native
    {
        [DllImport("s732std.dll", EntryPoint = "s7_get_device", ExactSpelling = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        internal static extern int S7_get_device(ushort index, ref ushort number_ptr, [MarshalAs(UnmanagedType.LPArray)] byte[] dev_name);

        [DllImport("s732std.dll", EntryPoint = "s7_get_vfd", ExactSpelling = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        internal static extern int S7_get_vfd([MarshalAs(UnmanagedType.LPArray)] byte[] dev_name, ushort index, ref ushort number_ptr, [MarshalAs(UnmanagedType.LPArray)] byte[] vfd_name);

        [DllImport("s732std.dll", EntryPoint = "s7_init", ExactSpelling = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        internal static extern int S7_init([MarshalAs(UnmanagedType.LPArray)] byte[] cp_pame, [MarshalAs(UnmanagedType.LPArray)] byte[] vfd_name, ref uint cp_descr_ptr);

        [DllImport("s732std.dll", EntryPoint = "s7_shut", ExactSpelling = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        internal static extern int S7_shut(uint cp_descr);

        [DllImport("s732std.dll", EntryPoint = "s7_get_cref", ExactSpelling = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        internal static extern int S7_get_cref(uint cp_descr, [MarshalAs(UnmanagedType.LPArray)] byte[] conn_pame, ref ushort cref_ptr);

        [DllImport("s732std.dll", EntryPoint = "s7_get_conn", ExactSpelling = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        internal static extern int S7_get_conn(uint cp_descr, ushort index, ref ushort number_ptr, ref ushort cref_ptr, [MarshalAs(UnmanagedType.LPArray)] byte[] conn_name);

        [DllImport("s732std.dll", EntryPoint = "s7_multiple_read", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int S7_multiple_read(uint cp_descr, ushort cref_ptr, ushort order_id, ushort number, S7ReadPara[] val_value_ptr);

        [DllImport("s732std.dll", EntryPoint = "s7_get_multiple_read_cnf", SetLastError= true, ExactSpelling = true, CharSet= CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int S7_get_multiple_read_cnf(IntPtr od_ptr, ref ushort msg_array_pointer, ref ushort val_length_ptr, byte[] val_value_ptr);

        [DllImport("s732std.dll", EntryPoint = "s7_read_req", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int S7_read_req(uint cp_descr, ushort cref_ptr, ushort order_id, S7ReadPara val_value_ptr);

        [DllImport("s732std.dll", EntryPoint = "s7_get_read_cnf", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int S7_get_read_cnf(uint cp_descr, ref ushort length, byte[] data);

        [DllImport("s732std.dll", EntryPoint = "s7_read_long_req", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int S7_read_long_req(uint cp_descr, ushort cref_ptr, ushort order_id, S7ReadPara val_value_ptr);

        [DllImport("s732std.dll", EntryPoint = "s7_get_write_cnf", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int S7_get_write_cnf();

        [DllImport("s732std.dll", EntryPoint = "s7_multiple_write_req", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int S7_multiple_write_req(uint cp_descr, ushort cref_ptr, ushort order_id, ushort number, S7WriteParaLong[] writepara, IntPtr od_ptr);


        [DllImport("s732std.dll", EntryPoint = "s7_receive", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int S7_receive(uint cp_descr, ref ushort cref_ptr, ref ushort order_id);


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct S7ReadPara
        {
            public UInt16 Access;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 34)]
            public byte[] VarName;
            public UInt16 Index;
            public UInt16 Subindex;
            public UInt16 Address_len;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] Address;
        };


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct S7WritePara
        {
            public UInt16 Access;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 34)]
            public byte[] VarName;
            public UInt16 Index;
            public UInt16 Subindex;
            public UInt16 Address_len;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] Address;
            public UInt16 VarLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public byte[] Value;
        };

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct S7WriteParaLong
        {
            public UInt16 Access;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 34)]
            public byte[] VarName;
            public UInt16 Index;
            public UInt16 Subindex;
            public UInt16 Address_len;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] Address;
            public UInt16 VarLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 932)]
            public byte[] Value;
        };


    }
}
