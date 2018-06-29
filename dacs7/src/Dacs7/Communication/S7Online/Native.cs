using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Dacs7.Communication.S7Online
{

    internal class Native
    {
        [DllImport("S7onlinx.dll")]
        public static extern int SetSinecHWnd(int handle, IntPtr hwnd);

        [DllImport("S7onlinx.dll")]
        public static extern int SetSinecHWndMsg(int handle, IntPtr hwnd, uint msg_id);

        [DllImport("S7onlinx.dll")]
        public static extern int SCP_open([MarshalAs(UnmanagedType.LPStr)] string name);

        [DllImport("S7onlinx.dll")]
        public static extern int SCP_close(int handle);

        [DllImport("S7onlinx.dll")]
        public static extern int SCP_send(int handle, ushort length, byte[] data);

        [DllImport("S7onlinx.dll")]
        public static extern int SCP_receive(int handle, ushort timeout, int[] recievendlength, ushort length, byte[] data);

        [DllImport("S7onlinx.dll")]
        public static extern int SCP_get_errno();


    }
}
