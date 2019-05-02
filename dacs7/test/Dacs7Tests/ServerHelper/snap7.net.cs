/*=============================================================================|
|  PROJECT SNAP7                                                         1.4.0 |
|==============================================================================|
|  Copyright (C) 2013, 2014, 2015 Davide Nardella                              |
|  All rights reserved.                                                        |
|==============================================================================|
|  SNAP7 is free software: you can redistribute it and/or modify               |
|  it under the terms of the Lesser GNU General Public License as published by |
|  the Free Software Foundation, either version 3 of the License, or           |
|  (at your option) any later version.                                         |
|                                                                              |
|  It means that you can distribute your commercial software linked with       |
|  SNAP7 without the requirement to distribute the source code of your         |
|  application and without the requirement that your application be itself     |
|  distributed under LGPL.                                                     |
|                                                                              |
|  SNAP7 is distributed in the hope that it will be useful,                    |
|  but WITHOUT ANY WARRANTY; without even the implied warranty of              |
|  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the               |
|  Lesser GNU General Public License for more details.                         |
|                                                                              |
|  You should have received a copy of the GNU General Public License and a     |
|  copy of Lesser GNU General Public License along with Snap7.                 |
|  If not, see  http://www.gnu.org/licenses/                                   |
|==============================================================================|
|                                                                              |
|  C# Interface classes.                                                       |
|                                                                              |
|=============================================================================*/
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Snap7
{

    public class S7Consts
    {
#if __MonoCS__  // Assuming that we are using Unix release of Mono (otherwise modify it)
            public const string Snap7LibName = "libsnap7.so";
#else
        public const string Snap7LibName = "snap7.dll";
#endif
        //------------------------------------------------------------------------------
        //                                  PARAMS LIST            
        //------------------------------------------------------------------------------
        public static readonly int p_u16_LocalPort = 1;
        public static readonly int p_u16_RemotePort = 2;
        public static readonly int p_i32_PingTimeout = 3;
        public static readonly int p_i32_SendTimeout = 4;
        public static readonly int p_i32_RecvTimeout = 5;
        public static readonly int p_i32_WorkInterval = 6;
        public static readonly int p_u16_SrcRef = 7;
        public static readonly int p_u16_DstRef = 8;
        public static readonly int p_u16_SrcTSap = 9;
        public static readonly int p_i32_PDURequest = 10;
        public static readonly int p_i32_MaxClients = 11;
        public static readonly int p_i32_BSendTimeout = 12;
        public static readonly int p_i32_BRecvTimeout = 13;
        public static readonly int p_u32_RecoveryTime = 14;
        public static readonly int p_u32_KeepAliveTime = 15;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct S7Tag
        {
            public int Area;
            public int DBNumber;
            public int Start;
            public int Elements;
            public int WordLen;
        }
    }

    public static class S7
    {
        #region [Help Functions]

        private static readonly long bias = 621355968000000000; // "decimicros" between 0001-01-01 00:00:00 and 1970-01-01 00:00:00

        private static int BCDtoByte(byte B) => ((B >> 4) * 10) + (B & 0x0F);

        private static byte ByteToBCD(int Value) => (byte)(((Value / 10) << 4) | (Value % 10));

        private static byte[] CopyFrom(byte[] Buffer, int Pos, int Size)
        {
            var Result = new byte[Size];
            Array.Copy(Buffer, Pos, Result, 0, Size);
            return Result;
        }

        #region Get/Set the bit at Pos.Bit
        public static bool GetBitAt(byte[] Buffer, int Pos, int Bit)
        {
            byte[] Mask = { 0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80 };
            if (Bit < 0) Bit = 0;
            if (Bit > 7) Bit = 7;
            return (Buffer[Pos] & Mask[Bit]) != 0;
        }
        public static void SetBitAt(ref byte[] Buffer, int Pos, int Bit, bool Value)
        {
            byte[] Mask = { 0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80 };
            if (Bit < 0) Bit = 0;
            if (Bit > 7) Bit = 7;

            if (Value)
                Buffer[Pos] = (byte)(Buffer[Pos] | Mask[Bit]);
            else
                Buffer[Pos] = (byte)(Buffer[Pos] & ~Mask[Bit]);
        }
        #endregion

        #region Get/Set 8 bit signed value (S7 SInt) -128..127
        public static int GetSIntAt(byte[] Buffer, int Pos)
        {
            int Value = Buffer[Pos];
            if (Value < 128)
                return Value;
            else
                return Value - 256;
        }
        public static void SetSIntAt(byte[] Buffer, int Pos, int Value)
        {
            if (Value < -128) Value = -128;
            if (Value > 127) Value = 127;
            Buffer[Pos] = (byte)Value;
        }
        #endregion

        #region Get/Set 16 bit signed value (S7 int) -32768..32767
        public static int GetIntAt(byte[] Buffer, int Pos) => (short)((Buffer[Pos] << 8) | Buffer[Pos + 1]);
        public static void SetIntAt(byte[] Buffer, int Pos, short Value)
        {
            Buffer[Pos] = (byte)(Value >> 8);
            Buffer[Pos + 1] = (byte)(Value & 0x00FF);
        }
        #endregion

        #region Get/Set 32 bit signed value (S7 DInt) -2147483648..2147483647
        public static int GetDIntAt(byte[] Buffer, int Pos)
        {
            int Result;
            Result = Buffer[Pos]; Result <<= 8;
            Result += Buffer[Pos + 1]; Result <<= 8;
            Result += Buffer[Pos + 2]; Result <<= 8;
            Result += Buffer[Pos + 3];
            return Result;
        }
        public static void SetDIntAt(byte[] Buffer, int Pos, int Value)
        {
            Buffer[Pos + 3] = (byte)(Value & 0xFF);
            Buffer[Pos + 2] = (byte)((Value >> 8) & 0xFF);
            Buffer[Pos + 1] = (byte)((Value >> 16) & 0xFF);
            Buffer[Pos] = (byte)((Value >> 24) & 0xFF);
        }
        #endregion

        #region Get/Set 64 bit signed value (S7 LInt) -9223372036854775808..9223372036854775807
        public static long GetLIntAt(byte[] Buffer, int Pos)
        {
            long Result;
            Result = Buffer[Pos]; Result <<= 8;
            Result += Buffer[Pos + 1]; Result <<= 8;
            Result += Buffer[Pos + 2]; Result <<= 8;
            Result += Buffer[Pos + 3]; Result <<= 8;
            Result += Buffer[Pos + 4]; Result <<= 8;
            Result += Buffer[Pos + 5]; Result <<= 8;
            Result += Buffer[Pos + 6]; Result <<= 8;
            Result += Buffer[Pos + 7];
            return Result;
        }
        public static void SetLIntAt(byte[] Buffer, int Pos, long Value)
        {
            Buffer[Pos + 7] = (byte)(Value & 0xFF);
            Buffer[Pos + 6] = (byte)((Value >> 8) & 0xFF);
            Buffer[Pos + 5] = (byte)((Value >> 16) & 0xFF);
            Buffer[Pos + 4] = (byte)((Value >> 24) & 0xFF);
            Buffer[Pos + 3] = (byte)((Value >> 32) & 0xFF);
            Buffer[Pos + 2] = (byte)((Value >> 40) & 0xFF);
            Buffer[Pos + 1] = (byte)((Value >> 48) & 0xFF);
            Buffer[Pos] = (byte)((Value >> 56) & 0xFF);
        }
        #endregion 

        #region Get/Set 8 bit unsigned value (S7 USInt) 0..255
        public static byte GetUSIntAt(byte[] Buffer, int Pos) => Buffer[Pos];
        public static void SetUSIntAt(byte[] Buffer, int Pos, byte Value) => Buffer[Pos] = Value;
        #endregion

        #region Get/Set 16 bit unsigned value (S7 UInt) 0..65535
        public static ushort GetUIntAt(byte[] Buffer, int Pos) => (ushort)((Buffer[Pos] << 8) | Buffer[Pos + 1]);
        public static void SetUIntAt(byte[] Buffer, int Pos, ushort Value)
        {
            Buffer[Pos] = (byte)(Value >> 8);
            Buffer[Pos + 1] = (byte)(Value & 0x00FF);
        }
        #endregion

        #region Get/Set 32 bit unsigned value (S7 UDInt) 0..4294967296
        public static uint GetUDIntAt(byte[] Buffer, int Pos)
        {
            uint Result;
            Result = Buffer[Pos]; Result <<= 8;
            Result |= Buffer[Pos + 1]; Result <<= 8;
            Result |= Buffer[Pos + 2]; Result <<= 8;
            Result |= Buffer[Pos + 3];
            return Result;
        }
        public static void SetUDIntAt(byte[] Buffer, int Pos, uint Value)
        {
            Buffer[Pos + 3] = (byte)(Value & 0xFF);
            Buffer[Pos + 2] = (byte)((Value >> 8) & 0xFF);
            Buffer[Pos + 1] = (byte)((Value >> 16) & 0xFF);
            Buffer[Pos] = (byte)((Value >> 24) & 0xFF);
        }
        #endregion

        #region Get/Set 64 bit unsigned value (S7 ULint) 0..18446744073709551616
        public static ulong GetULIntAt(byte[] Buffer, int Pos)
        {
            ulong Result;
            Result = Buffer[Pos]; Result <<= 8;
            Result |= Buffer[Pos + 1]; Result <<= 8;
            Result |= Buffer[Pos + 2]; Result <<= 8;
            Result |= Buffer[Pos + 3]; Result <<= 8;
            Result |= Buffer[Pos + 4]; Result <<= 8;
            Result |= Buffer[Pos + 5]; Result <<= 8;
            Result |= Buffer[Pos + 6]; Result <<= 8;
            Result |= Buffer[Pos + 7];
            return Result;
        }
        public static void SetULintAt(byte[] Buffer, int Pos, ulong Value)
        {
            Buffer[Pos + 7] = (byte)(Value & 0xFF);
            Buffer[Pos + 6] = (byte)((Value >> 8) & 0xFF);
            Buffer[Pos + 5] = (byte)((Value >> 16) & 0xFF);
            Buffer[Pos + 4] = (byte)((Value >> 24) & 0xFF);
            Buffer[Pos + 3] = (byte)((Value >> 32) & 0xFF);
            Buffer[Pos + 2] = (byte)((Value >> 40) & 0xFF);
            Buffer[Pos + 1] = (byte)((Value >> 48) & 0xFF);
            Buffer[Pos] = (byte)((Value >> 56) & 0xFF);
        }
        #endregion

        #region Get/Set 8 bit word (S7 Byte) 16#00..16#FF
        public static byte GetByteAt(byte[] Buffer, int Pos) => Buffer[Pos];
        public static void SetByteAt(byte[] Buffer, int Pos, byte Value) => Buffer[Pos] = Value;
        #endregion

        #region Get/Set 16 bit word (S7 Word) 16#0000..16#FFFF
        public static ushort GetWordAt(byte[] Buffer, int Pos) => GetUIntAt(Buffer, Pos);
        public static void SetWordAt(byte[] Buffer, int Pos, ushort Value) => SetUIntAt(Buffer, Pos, Value);
        #endregion

        #region Get/Set 32 bit word (S7 DWord) 16#00000000..16#FFFFFFFF
        public static uint GetDWordAt(byte[] Buffer, int Pos) => GetUDIntAt(Buffer, Pos);
        public static void SetDWordAt(byte[] Buffer, int Pos, uint Value) => SetUDIntAt(Buffer, Pos, Value);
        #endregion

        #region Get/Set 64 bit word (S7 LWord) 16#0000000000000000..16#FFFFFFFFFFFFFFFF
        public static ulong GetLWordAt(byte[] Buffer, int Pos) => GetULIntAt(Buffer, Pos);
        public static void SetLWordAt(byte[] Buffer, int Pos, ulong Value) => SetULintAt(Buffer, Pos, Value);
        #endregion

        #region Get/Set 32 bit floating point number (S7 Real) (Range of Single)
        public static float GetRealAt(byte[] Buffer, int Pos)
        {
            var Value = GetUDIntAt(Buffer, Pos);
            var bytes = BitConverter.GetBytes(Value);
            return BitConverter.ToSingle(bytes, 0);
        }
        public static void SetRealAt(byte[] Buffer, int Pos, float Value)
        {
            var FloatArray = BitConverter.GetBytes(Value);
            Buffer[Pos] = FloatArray[3];
            Buffer[Pos + 1] = FloatArray[2];
            Buffer[Pos + 2] = FloatArray[1];
            Buffer[Pos + 3] = FloatArray[0];
        }
        #endregion

        #region Get/Set 64 bit floating point number (S7 LReal) (Range of Double)
        public static double GetLRealAt(byte[] Buffer, int Pos)
        {
            var Value = GetULIntAt(Buffer, Pos);
            var bytes = BitConverter.GetBytes(Value);
            return BitConverter.ToDouble(bytes, 0);
        }
        public static void SetLRealAt(byte[] Buffer, int Pos, double Value)
        {
            var FloatArray = BitConverter.GetBytes(Value);
            Buffer[Pos] = FloatArray[7];
            Buffer[Pos + 1] = FloatArray[6];
            Buffer[Pos + 2] = FloatArray[5];
            Buffer[Pos + 3] = FloatArray[4];
            Buffer[Pos + 4] = FloatArray[3];
            Buffer[Pos + 5] = FloatArray[2];
            Buffer[Pos + 6] = FloatArray[1];
            Buffer[Pos + 7] = FloatArray[0];
        }
        #endregion

        #region Get/Set DateTime (S7 DATE_AND_TIME)
        public static DateTime GetDateTimeAt(byte[] Buffer, int Pos)
        {
            int Year, Month, Day, Hour, Min, Sec, MSec;

            Year = BCDtoByte(Buffer[Pos]);
            if (Year < 90)
                Year += 2000;
            else
                Year += 1900;

            Month = BCDtoByte(Buffer[Pos + 1]);
            Day = BCDtoByte(Buffer[Pos + 2]);
            Hour = BCDtoByte(Buffer[Pos + 3]);
            Min = BCDtoByte(Buffer[Pos + 4]);
            Sec = BCDtoByte(Buffer[Pos + 5]);
            MSec = (BCDtoByte(Buffer[Pos + 6]) * 10) + (BCDtoByte(Buffer[Pos + 7]) / 10);
            try
            {
                return new DateTime(Year, Month, Day, Hour, Min, Sec, MSec);
            }
            catch (System.ArgumentOutOfRangeException)
            {
                return new DateTime(0);
            }
        }
        public static void SetDateTimeAt(byte[] Buffer, int Pos, DateTime Value)
        {
            var Year = Value.Year;
            var Month = Value.Month;
            var Day = Value.Day;
            var Hour = Value.Hour;
            var Min = Value.Minute;
            var Sec = Value.Second;
            var Dow = (int)Value.DayOfWeek + 1;
            // MSecH = First two digits of miliseconds 
            var MsecH = Value.Millisecond / 10;
            // MSecL = Last digit of miliseconds
            var MsecL = Value.Millisecond % 10;
            if (Year > 1999)
                Year -= 2000;

            Buffer[Pos] = ByteToBCD(Year);
            Buffer[Pos + 1] = ByteToBCD(Month);
            Buffer[Pos + 2] = ByteToBCD(Day);
            Buffer[Pos + 3] = ByteToBCD(Hour);
            Buffer[Pos + 4] = ByteToBCD(Min);
            Buffer[Pos + 5] = ByteToBCD(Sec);
            Buffer[Pos + 6] = ByteToBCD(MsecH);
            Buffer[Pos + 7] = ByteToBCD(MsecL * 10 + Dow);
        }
        #endregion

        #region Get/Set DATE (S7 DATE) 
        public static DateTime GetDateAt(byte[] Buffer, int Pos)
        {
            try
            {
                return new DateTime(1990, 1, 1).AddDays(GetIntAt(Buffer, Pos));
            }
            catch (System.ArgumentOutOfRangeException)
            {
                return new DateTime(0);
            }
        }
        public static void SetDateAt(byte[] Buffer, int Pos, DateTime Value) => SetIntAt(Buffer, Pos, (short)(Value - new DateTime(1990, 1, 1)).Days);

        #endregion

        #region Get/Set TOD (S7 TIME_OF_DAY)
        public static DateTime GetTODAt(byte[] Buffer, int Pos)
        {
            try
            {
                return new DateTime(0).AddMilliseconds(S7.GetDIntAt(Buffer, Pos));
            }
            catch (System.ArgumentOutOfRangeException)
            {
                return new DateTime(0);
            }
        }
        public static void SetTODAt(byte[] Buffer, int Pos, DateTime Value)
        {
            var Time = Value.TimeOfDay;
            SetDIntAt(Buffer, Pos, (int)Math.Round(Time.TotalMilliseconds));
        }
        #endregion

        #region Get/Set LTOD (S7 1500 LONG TIME_OF_DAY)
        public static DateTime GetLTODAt(byte[] Buffer, int Pos)
        {
            // .NET Tick = 100 ns, S71500 Tick = 1 ns
            try
            {
                return new DateTime(Math.Abs(GetLIntAt(Buffer, Pos) / 100));
            }
            catch (System.ArgumentOutOfRangeException)
            {
                return new DateTime(0);
            }
        }
        public static void SetLTODAt(byte[] Buffer, int Pos, DateTime Value)
        {
            var Time = Value.TimeOfDay;
            SetLIntAt(Buffer, Pos, Time.Ticks * 100);
        }

        #endregion

        #region GET/SET LDT (S7 1500 Long Date and Time)
        public static DateTime GetLDTAt(byte[] Buffer, int Pos)
        {
            try
            {
                return new DateTime((GetLIntAt(Buffer, Pos) / 100) + bias);
            }
            catch (System.ArgumentOutOfRangeException)
            {
                return new DateTime(0);
            }
        }
        public static void SetLDTAt(byte[] Buffer, int Pos, DateTime Value) => SetLIntAt(Buffer, Pos, (Value.Ticks - bias) * 100);
        #endregion

        #region Get/Set DTL (S71200/1500 Date and Time)
        // Thanks to Johan Cardoen for GetDTLAt
        public static DateTime GetDTLAt(byte[] Buffer, int Pos)
        {
            int Year, Month, Day, Hour, Min, Sec, MSec;

            Year = Buffer[Pos] * 256 + Buffer[Pos + 1];
            Month = Buffer[Pos + 2];
            Day = Buffer[Pos + 3];
            Hour = Buffer[Pos + 5];
            Min = Buffer[Pos + 6];
            Sec = Buffer[Pos + 7];
            MSec = (int)GetUDIntAt(Buffer, Pos + 8) / 1000000;

            try
            {
                return new DateTime(Year, Month, Day, Hour, Min, Sec, MSec);
            }
            catch (System.ArgumentOutOfRangeException)
            {
                return new DateTime(0);
            }
        }
        public static void SetDTLAt(byte[] Buffer, int Pos, DateTime Value)
        {
            var Year = (short)Value.Year;
            var Month = (byte)Value.Month;
            var Day = (byte)Value.Day;
            var Hour = (byte)Value.Hour;
            var Min = (byte)Value.Minute;
            var Sec = (byte)Value.Second;
            var Dow = (byte)(Value.DayOfWeek + 1);

            var NanoSecs = Value.Millisecond * 1000000;

            var bytes_short = BitConverter.GetBytes(Year);

            Buffer[Pos] = bytes_short[1];
            Buffer[Pos + 1] = bytes_short[0];
            Buffer[Pos + 2] = Month;
            Buffer[Pos + 3] = Day;
            Buffer[Pos + 4] = Dow;
            Buffer[Pos + 5] = Hour;
            Buffer[Pos + 6] = Min;
            Buffer[Pos + 7] = Sec;
            SetDIntAt(Buffer, Pos + 8, NanoSecs);
        }
        #endregion

        #region Get/Set String (S7 String)
        // Thanks to Pablo Agirre 
        public static string GetStringAt(byte[] Buffer, int Pos)
        {
            var size = (int)Buffer[Pos + 1];
            return Encoding.ASCII.GetString(Buffer, Pos + 2, size);
        }
        public static void SetStringAt(byte[] Buffer, int Pos, int MaxLen, string Value)
        {
            var size = Value.Length;
            Buffer[Pos] = (byte)MaxLen;
            Buffer[Pos + 1] = (byte)size;
            Encoding.ASCII.GetBytes(Value, 0, size, Buffer, Pos + 2);
        }
        #endregion

        #region Get/Set Array of char (S7 ARRAY OF CHARS)
        public static string GetCharsAt(byte[] Buffer, int Pos, int Size) => Encoding.ASCII.GetString(Buffer, Pos, Size);
        public static void SetCharsAt(byte[] Buffer, int Pos, string Value)
        {
            var MaxLen = Buffer.Length - Pos;
            // Truncs the string if there's no room enough        
            if (MaxLen > Value.Length) MaxLen = Value.Length;
            Encoding.ASCII.GetBytes(Value, 0, MaxLen, Buffer, Pos);
        }
        #endregion

        #endregion [Help Functions]
    }

    public class S7MultiVar
    {
        #region [MultiRead/Write Helper]
        private readonly S7Client FClient;
        private readonly GCHandle[] Handles = new GCHandle[S7Client.MaxVars];
        private int Count = 0;
        private readonly S7Client.S7DataItem[] Items = new S7Client.S7DataItem[S7Client.MaxVars];

        public int[] Results = new int[S7Client.MaxVars];

        public S7MultiVar(S7Client Client)
        {
            FClient = Client;
            for (var c = 0; c < S7Client.MaxVars; c++)
                Results[c] = (int)S7Client.errCliItemNotAvailable;
        }
        ~S7MultiVar()
        {
            Clear();
        }

        public bool Add<T>(int Area, int WordLen, int DBNumber, int Start, int Amount, ref T[] Buffer) => Add(Area, WordLen, DBNumber, Start, Amount, ref Buffer, 0);

        public bool Add<T>(int Area, int WordLen, int DBNumber, int Start, int Amount, ref T[] Buffer, int Offset)
        {
            if (Count < S7Client.MaxVars)
            {
                Items[Count].Area = Area;
                Items[Count].WordLen = WordLen;
                Items[Count].Result = 0;
                Items[Count].DBNumber = DBNumber;
                Items[Count].Start = Start;
                Items[Count].Amount = Amount;
                var handle = GCHandle.Alloc(Buffer, GCHandleType.Pinned);
                if (IntPtr.Size == 4)
                    Items[Count].pData = (IntPtr)(handle.AddrOfPinnedObject().ToInt32() + Offset * Marshal.SizeOf(typeof(T)));
                else
                    Items[Count].pData = (IntPtr)(handle.AddrOfPinnedObject().ToInt64() + Offset * Marshal.SizeOf(typeof(T)));

                Handles[Count] = handle;
                Count++;
                return true;
            }
            else
            {
                return false;
            }
        }

        public int Read()
        {
            int FunctionResult;
            if (Count > 0)
            {
                FunctionResult = FClient.ReadMultiVars(Items, Count);
                if (FunctionResult == 0)
                {
                    for (var c = 0; c < S7Client.MaxVars; c++)
                        Results[c] = Items[c].Result;
                }

                return FunctionResult;
            }
            else
            {
                return (int)S7Client.errCliFunctionRefused;
            }
        }
        public int Write()
        {
            int FunctionResult;
            if (Count > 0)
            {
                FunctionResult = FClient.WriteMultiVars(Items, Count);
                if (FunctionResult == 0)
                {
                    for (var c = 0; c < S7Client.MaxVars; c++)
                        Results[c] = Items[c].Result;
                }

                return FunctionResult;
            }
            else
            {
                return (int)S7Client.errCliFunctionRefused;
            }
        }
        public void Clear()
        {
            for (var c = 0; c < Count; c++)
            {
                if (Handles[c] != null)
                    Handles[c].Free();
            }
            for (var c = 0; c < S7Client.MaxVars; c++)
                Results[c] = (int)S7Client.errCliItemNotAvailable;
            Count = 0;
        }
        #endregion
    }

    public class S7Client
    {
        #region [Constants, private vars and TypeDefs]
        private const int MsgTextLen = 1024;
        // Error codes
        public static readonly uint errNegotiatingPDU = 0x00100000;
        public static readonly uint errCliInvalidParams = 0x00200000;
        public static readonly uint errCliJobPending = 0x00300000;
        public static readonly uint errCliTooManyItems = 0x00400000;
        public static readonly uint errCliInvalidWordLen = 0x00500000;
        public static readonly uint errCliPartialDataWritten = 0x00600000;
        public static readonly uint errCliSizeOverPDU = 0x00700000;
        public static readonly uint errCliInvalidPlcAnswer = 0x00800000;
        public static readonly uint errCliAddressOutOfRange = 0x00900000;
        public static readonly uint errCliInvalidTransportSize = 0x00A00000;
        public static readonly uint errCliWriteDataSizeMismatch = 0x00B00000;
        public static readonly uint errCliItemNotAvailable = 0x00C00000;
        public static readonly uint errCliInvalidValue = 0x00D00000;
        public static readonly uint errCliCannotStartPLC = 0x00E00000;
        public static readonly uint errCliAlreadyRun = 0x00F00000;
        public static readonly uint errCliCannotStopPLC = 0x01000000;
        public static readonly uint errCliCannotCopyRamToRom = 0x01100000;
        public static readonly uint errCliCannotCompress = 0x01200000;
        public static readonly uint errCliAlreadyStop = 0x01300000;
        public static readonly uint errCliFunNotAvailable = 0x01400000;
        public static readonly uint errCliUploadSequenceFailed = 0x01500000;
        public static readonly uint errCliInvalidDataSizeRecvd = 0x01600000;
        public static readonly uint errCliInvalidBlockType = 0x01700000;
        public static readonly uint errCliInvalidBlockNumber = 0x01800000;
        public static readonly uint errCliInvalidBlockSize = 0x01900000;
        public static readonly uint errCliDownloadSequenceFailed = 0x01A00000;
        public static readonly uint errCliInsertRefused = 0x01B00000;
        public static readonly uint errCliDeleteRefused = 0x01C00000;
        public static readonly uint errCliNeedPassword = 0x01D00000;
        public static readonly uint errCliInvalidPassword = 0x01E00000;
        public static readonly uint errCliNoPasswordToSetOrClear = 0x01F00000;
        public static readonly uint errCliJobTimeout = 0x02000000;
        public static readonly uint errCliPartialDataRead = 0x02100000;
        public static readonly uint errCliBufferTooSmall = 0x02200000;
        public static readonly uint errCliFunctionRefused = 0x02300000;
        public static readonly uint errCliDestroying = 0x02400000;
        public static readonly uint errCliInvalidParamNumber = 0x02500000;
        public static readonly uint errCliCannotChangeParam = 0x02600000;

        // Area ID
        public static readonly byte S7AreaPE = 0x81;
        public static readonly byte S7AreaPA = 0x82;
        public static readonly byte S7AreaMK = 0x83;
        public static readonly byte S7AreaDB = 0x84;
        public static readonly byte S7AreaCT = 0x1C;
        public static readonly byte S7AreaTM = 0x1D;

        // Word Length
        public static readonly int S7WLBit = 0x01;
        public static readonly int S7WLByte = 0x02;
        public static readonly int S7WLWord = 0x04;
        public static readonly int S7WLDWord = 0x06;
        public static readonly int S7WLReal = 0x08;
        public static readonly int S7WLCounter = 0x1C;
        public static readonly int S7WLTimer = 0x1D;

        // Block type
        public static readonly byte Block_OB = 0x38;
        public static readonly byte Block_DB = 0x41;
        public static readonly byte Block_SDB = 0x42;
        public static readonly byte Block_FC = 0x43;
        public static readonly byte Block_SFC = 0x44;
        public static readonly byte Block_FB = 0x45;
        public static readonly byte Block_SFB = 0x46;

        // Sub Block Type 
        public static readonly byte SubBlk_OB = 0x08;
        public static readonly byte SubBlk_DB = 0x0A;
        public static readonly byte SubBlk_SDB = 0x0B;
        public static readonly byte SubBlk_FC = 0x0C;
        public static readonly byte SubBlk_SFC = 0x0D;
        public static readonly byte SubBlk_FB = 0x0E;
        public static readonly byte SubBlk_SFB = 0x0F;

        // Block languages
        public static readonly byte BlockLangAWL = 0x01;
        public static readonly byte BlockLangKOP = 0x02;
        public static readonly byte BlockLangFUP = 0x03;
        public static readonly byte BlockLangSCL = 0x04;
        public static readonly byte BlockLangDB = 0x05;
        public static readonly byte BlockLangGRAPH = 0x06;

        // Max number of vars (multiread/write)
        public static readonly int MaxVars = 20;

        // Client Connection Type
        public static readonly ushort CONNTYPE_PG = 0x01;  // Connect to the PLC as a PG
        public static readonly ushort CONNTYPE_OP = 0x02;  // Connect to the PLC as an OP
        public static readonly ushort CONNTYPE_BASIC = 0x03;  // Basic connection 

        // Job
        private const int JobComplete = 0;
        private const int JobPending = 1;

        private IntPtr Client;

        // New Data Item, Thanks to LanceL
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct S7DataItem
        {
            public int Area;
            public int WordLen;
            public int Result;
            public int DBNumber;
            public int Start;
            public int Amount;
            public IntPtr pData;
        }

        // Block List
        [StructLayout(LayoutKind.Sequential, Pack = 1)] // <- "maybe" we don't need
        public struct S7BlocksList
        {
            public int OBCount;
            public int FBCount;
            public int FCCount;
            public int SFBCount;
            public int SFCCount;
            public int DBCount;
            public int SDBCount;
        };

        // Packed Block Info
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        protected struct US7BlockInfo
        {
            public int BlkType;
            public int BlkNumber;
            public int BlkLang;
            public int BlkFlags;
            public int MC7Size;  // The real size in bytes
            public int LoadSize;
            public int LocalData;
            public int SBBLength;
            public int CheckSum;
            public int Version;
            // Chars info
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 11)]
            public char[] CodeDate;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 11)]
            public char[] IntfDate;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
            public char[] Author;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
            public char[] Family;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
            public char[] Header;
        };
        private US7BlockInfo UBlockInfo;

        // Managed Block Info
        public struct S7BlockInfo
        {
            public int BlkType;
            public int BlkNumber;
            public int BlkLang;
            public int BlkFlags;
            public int MC7Size;  // The real size in bytes
            public int LoadSize;
            public int LocalData;
            public int SBBLength;
            public int CheckSum;
            public int Version;
            // Chars info
            public string CodeDate;
            public string IntfDate;
            public string Author;
            public string Family;
            public string Header;
        };

        public ushort[] TS7BlocksOfType;

        // Packed Order Code + Version
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        protected struct US7OrderCode
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 21)]
            public char[] Code;
            public byte V1;
            public byte V2;
            public byte V3;
        };
        private US7OrderCode UOrderCode;

        // Managed Order Code + Version
        public struct S7OrderCode
        {
            public string Code;
            public byte V1;
            public byte V2;
            public byte V3;
        };

        // Packed CPU Info
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        protected struct US7CpuInfo
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 33)]
            public char[] ModuleTypeName;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
            public char[] SerialNumber;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
            public char[] ASName;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 27)]
            public char[] Copyright;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
            public char[] ModuleName;
        };
        private US7CpuInfo UCpuInfo;

        // Managed CPU Info
        public struct S7CpuInfo
        {
            public string ModuleTypeName;
            public string SerialNumber;
            public string ASName;
            public string Copyright;
            public string ModuleName;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct S7CpInfo
        {
            public int MaxPduLengt;
            public int MaxConnections;
            public int MaxMpiRate;
            public int MaxBusRate;
        };

        // See §33.1 of "System Software for S7-300/400 System and Standard Functions"
        // and see SFC51 description too
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SZL_HEADER
        {
            public ushort LENTHDR;
            public ushort N_DR;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct S7SZL
        {
            public SZL_HEADER Header;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x4000 - 4)]
            public byte[] Data;
        };

        // SZL List of available SZL IDs : same as SZL but List items are big-endian adjusted
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct S7SZLList
        {
            public SZL_HEADER Header;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x2000 - 2)]
            public ushort[] Data;
        };

        // S7 Protection
        // See §33.19 of "System Software for S7-300/400 System and Standard Functions"
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct S7Protection  // Packed S7Protection
        {
            public ushort sch_schal;
            public ushort sch_par;
            public ushort sch_rel;
            public ushort bart_sch;
            public ushort anl_sch;
        };

        // C++ time struct, functions to convert it from/to DateTime are provided ;-)
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        protected struct cpp_tm
        {
            public int tm_sec;
            public int tm_min;
            public int tm_hour;
            public int tm_mday;
            public int tm_mon;
            public int tm_year;
            public int tm_wday;
            public int tm_yday;
            public int tm_isdst;
        }
        private cpp_tm tm;

        #endregion

        #region [Class Control]

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern IntPtr Cli_Create();
        public S7Client() => Client = Cli_Create();

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_Destroy(ref IntPtr Client);
        ~S7Client()
        {
            Cli_Destroy(ref Client);
        }

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_Connect(IntPtr Client);
        public int Connect() => Cli_Connect(Client);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_ConnectTo(IntPtr Client,
            [MarshalAs(UnmanagedType.LPStr)] string Address,
            int Rack,
            int Slot
            );

        public int ConnectTo(string Address, int Rack, int Slot) => Cli_ConnectTo(Client, Address, Rack, Slot);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_SetConnectionParams(IntPtr Client,
            [MarshalAs(UnmanagedType.LPStr)] string Address,
            ushort LocalTSAP,
            ushort RemoteTSAP
            );

        public int SetConnectionParams(string Address, ushort LocalTSAP, ushort RemoteTSAP) => Cli_SetConnectionParams(Client, Address, LocalTSAP, RemoteTSAP);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_SetConnectionType(IntPtr Client, ushort ConnectionType);
        public int SetConnectionType(ushort ConnectionType) => Cli_SetConnectionType(Client, ConnectionType);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_Disconnect(IntPtr Client);
        public int Disconnect() => Cli_Disconnect(Client);

        // Get/SetParam needs a void* parameter, internally it decides the kind of pointer
        // in accord to ParamNumber.
        // To avoid the use of unsafe code we split the DLL functions and use overloaded methods.

        [DllImport(S7Consts.Snap7LibName, EntryPoint = "Cli_GetParam")]
        protected static extern int Cli_GetParam_i16(IntPtr Client, int ParamNumber, ref short IntValue);
        public int GetParam(int ParamNumber, ref short IntValue) => Cli_GetParam_i16(Client, ParamNumber, ref IntValue);

        [DllImport(S7Consts.Snap7LibName, EntryPoint = "Cli_GetParam")]
        protected static extern int Cli_GetParam_u16(IntPtr Client, int ParamNumber, ref ushort IntValue);
        public int GetParam(int ParamNumber, ref ushort IntValue) => Cli_GetParam_u16(Client, ParamNumber, ref IntValue);

        [DllImport(S7Consts.Snap7LibName, EntryPoint = "Cli_GetParam")]
        protected static extern int Cli_GetParam_i32(IntPtr Client, int ParamNumber, ref int IntValue);
        public int GetParam(int ParamNumber, ref int IntValue) => Cli_GetParam_i32(Client, ParamNumber, ref IntValue);

        [DllImport(S7Consts.Snap7LibName, EntryPoint = "Cli_GetParam")]
        protected static extern int Cli_GetParam_u32(IntPtr Client, int ParamNumber, ref uint IntValue);
        public int GetParam(int ParamNumber, ref uint IntValue) => Cli_GetParam_u32(Client, ParamNumber, ref IntValue);

        [DllImport(S7Consts.Snap7LibName, EntryPoint = "Cli_GetParam")]
        protected static extern int Cli_GetParam_i64(IntPtr Client, int ParamNumber, ref long IntValue);
        public int GetParam(int ParamNumber, ref long IntValue) => Cli_GetParam_i64(Client, ParamNumber, ref IntValue);

        [DllImport(S7Consts.Snap7LibName, EntryPoint = "Cli_GetParam")]
        protected static extern int Cli_GetParam_u64(IntPtr Client, int ParamNumber, ref ulong IntValue);
        public int GetParam(int ParamNumber, ref ulong IntValue) => Cli_GetParam_u64(Client, ParamNumber, ref IntValue);

        [DllImport(S7Consts.Snap7LibName, EntryPoint = "Cli_SetParam")]
        protected static extern int Cli_SetParam_i16(IntPtr Client, int ParamNumber, ref short IntValue);
        public int SetParam(int ParamNumber, ref short IntValue) => Cli_SetParam_i16(Client, ParamNumber, ref IntValue);

        [DllImport(S7Consts.Snap7LibName, EntryPoint = "Cli_SetParam")]
        protected static extern int Cli_SetParam_u16(IntPtr Client, int ParamNumber, ref ushort IntValue);
        public int SetParam(int ParamNumber, ref ushort IntValue) => Cli_SetParam_u16(Client, ParamNumber, ref IntValue);

        [DllImport(S7Consts.Snap7LibName, EntryPoint = "Cli_SetParam")]
        protected static extern int Cli_SetParam_i32(IntPtr Client, int ParamNumber, ref int IntValue);
        public int SetParam(int ParamNumber, ref int IntValue) => Cli_SetParam_i32(Client, ParamNumber, ref IntValue);

        [DllImport(S7Consts.Snap7LibName, EntryPoint = "Cli_SetParam")]
        protected static extern int Cli_SetParam_u32(IntPtr Client, int ParamNumber, ref uint IntValue);
        public int SetParam(int ParamNumber, ref uint IntValue) => Cli_SetParam_u32(Client, ParamNumber, ref IntValue);

        [DllImport(S7Consts.Snap7LibName, EntryPoint = "Cli_SetParam")]
        protected static extern int Cli_SetParam_i64(IntPtr Client, int ParamNumber, ref long IntValue);
        public int SetParam(int ParamNumber, ref long IntValue) => Cli_SetParam_i64(Client, ParamNumber, ref IntValue);

        [DllImport(S7Consts.Snap7LibName, EntryPoint = "Cli_SetParam")]
        protected static extern int Cli_SetParam_u64(IntPtr Client, int ParamNumber, ref ulong IntValue);
        public int SetParam(int ParamNumber, ref ulong IntValue) => Cli_SetParam_u64(Client, ParamNumber, ref IntValue);

        public delegate void S7CliCompletion(IntPtr usrPtr, int opCode, int opResult);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_SetAsCallback(IntPtr Client, S7CliCompletion Completion, IntPtr usrPtr);
        public int SetAsCallBack(S7CliCompletion Completion, IntPtr usrPtr) => Cli_SetAsCallback(Client, Completion, usrPtr);

        #endregion

        #region [Data I/O main functions]

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_ReadArea(IntPtr Client, int Area, int DBNumber, int Start, int Amount, int WordLen, byte[] Buffer);
        public int ReadArea(int Area, int DBNumber, int Start, int Amount, int WordLen, byte[] Buffer) => Cli_ReadArea(Client, Area, DBNumber, Start, Amount, WordLen, Buffer);

        [DllImport(S7Consts.Snap7LibName, EntryPoint = "Cli_ReadArea")]
        protected static extern int Cli_ReadArea_ptr(IntPtr Client, int Area, int DBNumber, int Start, int Amount, int WordLen, IntPtr Pointer);
        public int ReadArea(int Area, int DBNumber, int Start, int Amount, int WordLen, IntPtr Pointer) => Cli_ReadArea_ptr(Client, Area, DBNumber, Start, Amount, WordLen, Pointer);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_WriteArea(IntPtr Client, int Area, int DBNumber, int Start, int Amount, int WordLen, byte[] Buffer);
        public int WriteArea(int Area, int DBNumber, int Start, int Amount, int WordLen, byte[] Buffer) => Cli_WriteArea(Client, Area, DBNumber, Start, Amount, WordLen, Buffer);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_ReadMultiVars(IntPtr Client, ref S7DataItem Item, int ItemsCount);
        public int ReadMultiVars(S7DataItem[] Items, int ItemsCount) => Cli_ReadMultiVars(Client, ref Items[0], ItemsCount);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_WriteMultiVars(IntPtr Client, ref S7DataItem Item, int ItemsCount);
        public int WriteMultiVars(S7DataItem[] Items, int ItemsCount) => Cli_WriteMultiVars(Client, ref Items[0], ItemsCount);

        #endregion

        #region [Data I/O lean functions]

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_DBRead(IntPtr Client, int DBNumber, int Start, int Size, byte[] Buffer);
        public int DBRead(int DBNumber, int Start, int Size, byte[] Buffer) => Cli_DBRead(Client, DBNumber, Start, Size, Buffer);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_DBWrite(IntPtr Client, int DBNumber, int Start, int Size, byte[] Buffer);
        public int DBWrite(int DBNumber, int Start, int Size, byte[] Buffer) => Cli_DBWrite(Client, DBNumber, Start, Size, Buffer);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_MBRead(IntPtr Client, int Start, int Size, byte[] Buffer);
        public int MBRead(int Start, int Size, byte[] Buffer) => Cli_MBRead(Client, Start, Size, Buffer);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_MBWrite(IntPtr Client, int Start, int Size, byte[] Buffer);
        public int MBWrite(int Start, int Size, byte[] Buffer) => Cli_MBWrite(Client, Start, Size, Buffer);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_EBRead(IntPtr Client, int Start, int Size, byte[] Buffer);
        public int EBRead(int Start, int Size, byte[] Buffer) => Cli_EBRead(Client, Start, Size, Buffer);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_EBWrite(IntPtr Client, int Start, int Size, byte[] Buffer);
        public int EBWrite(int Start, int Size, byte[] Buffer) => Cli_EBWrite(Client, Start, Size, Buffer);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_ABRead(IntPtr Client, int Start, int Size, byte[] Buffer);
        public int ABRead(int Start, int Size, byte[] Buffer) => Cli_ABRead(Client, Start, Size, Buffer);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_ABWrite(IntPtr Client, int Start, int Size, byte[] Buffer);
        public int ABWrite(int Start, int Size, byte[] Buffer) => Cli_ABWrite(Client, Start, Size, Buffer);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_TMRead(IntPtr Client, int Start, int Amount, ushort[] Buffer);
        public int TMRead(int Start, int Amount, ushort[] Buffer) => Cli_TMRead(Client, Start, Amount, Buffer);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_TMWrite(IntPtr Client, int Start, int Amount, ushort[] Buffer);
        public int TMWrite(int Start, int Amount, ushort[] Buffer) => Cli_TMWrite(Client, Start, Amount, Buffer);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_CTRead(IntPtr Client, int Start, int Amount, ushort[] Buffer);
        public int CTRead(int Start, int Amount, ushort[] Buffer) => Cli_CTRead(Client, Start, Amount, Buffer);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_CTWrite(IntPtr Client, int Start, int Amount, ushort[] Buffer);
        public int CTWrite(int Start, int Amount, ushort[] Buffer) => Cli_CTWrite(Client, Start, Amount, Buffer);

        #endregion

        #region [Directory functions]

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_ListBlocks(IntPtr Client, ref S7BlocksList List);
        public int ListBlocks(ref S7BlocksList List) => Cli_ListBlocks(Client, ref List);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_GetAgBlockInfo(IntPtr Client, int BlockType, int BlockNum, ref US7BlockInfo Info);
        public int GetAgBlockInfo(int BlockType, int BlockNum, ref S7BlockInfo Info)
        {
            var res = Cli_GetAgBlockInfo(Client, BlockType, BlockNum, ref UBlockInfo);
            // Packed->Managed
            if (res == 0)
            {
                Info.BlkType = UBlockInfo.BlkType;
                Info.BlkNumber = UBlockInfo.BlkNumber;
                Info.BlkLang = UBlockInfo.BlkLang;
                Info.BlkFlags = UBlockInfo.BlkFlags;
                Info.MC7Size = UBlockInfo.MC7Size;
                Info.LoadSize = UBlockInfo.LoadSize;
                Info.LocalData = UBlockInfo.LocalData;
                Info.SBBLength = UBlockInfo.SBBLength;
                Info.CheckSum = UBlockInfo.CheckSum;
                Info.Version = UBlockInfo.Version;
                // Chars info
                Info.CodeDate = new string(UBlockInfo.CodeDate);
                Info.IntfDate = new string(UBlockInfo.IntfDate);
                Info.Author = new string(UBlockInfo.Author);
                Info.Family = new string(UBlockInfo.Family);
                Info.Header = new string(UBlockInfo.Header);
            }
            return res;
        }

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_GetPgBlockInfo(IntPtr Client, ref US7BlockInfo Info, byte[] Buffer, int Size);
        public int GetPgBlockInfo(ref S7BlockInfo Info, byte[] Buffer, int Size)
        {
            var res = Cli_GetPgBlockInfo(Client, ref UBlockInfo, Buffer, Size);
            // Packed->Managed
            if (res == 0)
            {
                Info.BlkType = UBlockInfo.BlkType;
                Info.BlkNumber = UBlockInfo.BlkNumber;
                Info.BlkLang = UBlockInfo.BlkLang;
                Info.BlkFlags = UBlockInfo.BlkFlags;
                Info.MC7Size = UBlockInfo.MC7Size;
                Info.LoadSize = UBlockInfo.LoadSize;
                Info.LocalData = UBlockInfo.LocalData;
                Info.SBBLength = UBlockInfo.SBBLength;
                Info.CheckSum = UBlockInfo.CheckSum;
                Info.Version = UBlockInfo.Version;
                // Chars info
                Info.CodeDate = new string(UBlockInfo.CodeDate);
                Info.IntfDate = new string(UBlockInfo.IntfDate);
                Info.Author = new string(UBlockInfo.Author);
                Info.Family = new string(UBlockInfo.Family);
                Info.Header = new string(UBlockInfo.Header);
            }
            return res;
        }

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_ListBlocksOfType(IntPtr Client, int BlockType, ushort[] List, ref int ItemsCount);
        public int ListBlocksOfType(int BlockType, ushort[] List, ref int ItemsCount) => Cli_ListBlocksOfType(Client, BlockType, List, ref ItemsCount);

        #endregion

        #region [Blocks functions]

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_Upload(IntPtr Client, int BlockType, int BlockNum, byte[] UsrData, ref int Size);
        public int Upload(int BlockType, int BlockNum, byte[] UsrData, ref int Size) => Cli_Upload(Client, BlockType, BlockNum, UsrData, ref Size);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_FullUpload(IntPtr Client, int BlockType, int BlockNum, byte[] UsrData, ref int Size);
        public int FullUpload(int BlockType, int BlockNum, byte[] UsrData, ref int Size) => Cli_FullUpload(Client, BlockType, BlockNum, UsrData, ref Size);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_Download(IntPtr Client, int BlockNum, byte[] UsrData, int Size);
        public int Download(int BlockNum, byte[] UsrData, int Size) => Cli_Download(Client, BlockNum, UsrData, Size);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_Delete(IntPtr Client, int BlockType, int BlockNum);
        public int Delete(int BlockType, int BlockNum) => Cli_Delete(Client, BlockType, BlockNum);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_DBGet(IntPtr Client, int DBNumber, byte[] UsrData, ref int Size);
        public int DBGet(int DBNumber, byte[] UsrData, ref int Size) => Cli_DBGet(Client, DBNumber, UsrData, ref Size);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_DBFill(IntPtr Client, int DBNumber, int FillChar);
        public int DBFill(int DBNumber, int FillChar) => Cli_DBFill(Client, DBNumber, FillChar);

        #endregion

        #region [Date/Time functions]

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_GetPlcDateTime(IntPtr Client, ref cpp_tm tm);
        public int GetPlcDateTime(ref DateTime DT)
        {
            var res = Cli_GetPlcDateTime(Client, ref tm);
            if (res == 0)
            {
                // Packed->Managed
                var PlcDT = new DateTime(tm.tm_year + 1900, tm.tm_mon + 1, tm.tm_mday, tm.tm_hour, tm.tm_min, tm.tm_sec);
                DT = PlcDT;
            }
            return res;
        }

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_SetPlcDateTime(IntPtr Client, ref cpp_tm tm);
        public int SetPlcDateTime(DateTime DT)
        {

            // Managed->Packed
            tm.tm_year = DT.Year - 1900;
            tm.tm_mon = DT.Month - 1;
            tm.tm_mday = DT.Day;
            tm.tm_hour = DT.Hour;
            tm.tm_min = DT.Minute;
            tm.tm_sec = DT.Second;

            return Cli_SetPlcDateTime(Client, ref tm);
        }

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_SetPlcSystemDateTime(IntPtr Client);
        public int SetPlcSystemDateTime() => Cli_SetPlcSystemDateTime(Client);

        #endregion

        #region [System Info functions]

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_GetOrderCode(IntPtr Client, ref US7OrderCode Info);
        public int GetOrderCode(ref S7OrderCode Info)
        {
            var res = Cli_GetOrderCode(Client, ref UOrderCode);
            // Packed->Managed
            if (res == 0)
            {
                Info.Code = new string(UOrderCode.Code);
                Info.V1 = UOrderCode.V1;
                Info.V2 = UOrderCode.V2;
                Info.V3 = UOrderCode.V3;
            }
            return res;
        }

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_GetCpuInfo(IntPtr Client, ref US7CpuInfo Info);
        public int GetCpuInfo(ref S7CpuInfo Info)
        {
            var res = Cli_GetCpuInfo(Client, ref UCpuInfo);
            // Packed->Managed
            if (res == 0)
            {
                Info.ModuleTypeName = new string(UCpuInfo.ModuleTypeName);
                Info.SerialNumber = new string(UCpuInfo.SerialNumber);
                Info.ASName = new string(UCpuInfo.ASName);
                Info.Copyright = new string(UCpuInfo.Copyright);
                Info.ModuleName = new string(UCpuInfo.ModuleName);
            }
            return res;
        }

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_GetCpInfo(IntPtr Client, ref S7CpInfo Info);

        public int GetCpInfo(ref S7CpInfo Info) => Cli_GetCpInfo(Client, ref Info);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_ReadSZL(IntPtr Client, int ID, int Index, ref S7SZL Data, ref int Size);
        public int ReadSZL(int ID, int Index, ref S7SZL Data, ref int Size) => Cli_ReadSZL(Client, ID, Index, ref Data, ref Size);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_ReadSZLList(IntPtr Client, ref S7SZLList List, ref int ItemsCount);
        public int ReadSZLList(ref S7SZLList List, ref int ItemsCount) => Cli_ReadSZLList(Client, ref List, ref ItemsCount);

        #endregion

        #region [Control functions]

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_PlcHotStart(IntPtr Client);
        public int PlcHotStart() => Cli_PlcHotStart(Client);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_PlcColdStart(IntPtr Client);
        public int PlcColdStart() => Cli_PlcColdStart(Client);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_PlcStop(IntPtr Client);
        public int PlcStop() => Cli_PlcStop(Client);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_CopyRamToRom(IntPtr Client, uint Timeout);
        public int PlcCopyRamToRom(uint Timeout) => Cli_CopyRamToRom(Client, Timeout);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_Compress(IntPtr Client, uint Timeout);
        public int PlcCompress(uint Timeout) => Cli_Compress(Client, Timeout);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_GetPlcStatus(IntPtr Client, ref int Status);
        public int PlcGetStatus(ref int Status) => Cli_GetPlcStatus(Client, ref Status);

        #endregion

        #region [Security functions]

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_GetProtection(IntPtr Client, ref S7Protection Protection);
        public int GetProtection(ref S7Protection Protection) => Cli_GetProtection(Client, ref Protection);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_SetSessionPassword(IntPtr Client, [MarshalAs(UnmanagedType.LPStr)] string Password);
        public int SetSessionPassword(string Password) => Cli_SetSessionPassword(Client, Password);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_ClearSessionPassword(IntPtr Client);
        public int ClearSessionPassword() => Cli_ClearSessionPassword(Client);

        #endregion

        #region [Low Level]

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_IsoExchangeBuffer(IntPtr Client, byte[] Buffer, ref int Size);
        public int IsoExchangeBuffer(byte[] Buffer, ref int Size) => Cli_IsoExchangeBuffer(Client, Buffer, ref Size);

        #endregion

        #region [Async functions]

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_AsReadArea(IntPtr Client, int Area, int DBNumber, int Start, int Amount, int WordLen, byte[] Buffer);
        public int AsReadArea(int Area, int DBNumber, int Start, int Amount, int WordLen, byte[] Buffer) => Cli_AsReadArea(Client, Area, DBNumber, Start, Amount, WordLen, Buffer);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_AsWriteArea(IntPtr Client, int Area, int DBNumber, int Start, int Amount, int WordLen, byte[] Buffer);
        public int AsWriteArea(int Area, int DBNumber, int Start, int Amount, int WordLen, byte[] Buffer) => Cli_AsWriteArea(Client, Area, DBNumber, Start, Amount, WordLen, Buffer);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_AsDBRead(IntPtr Client, int DBNumber, int Start, int Size, byte[] Buffer);
        public int AsDBRead(int DBNumber, int Start, int Size, byte[] Buffer) => Cli_AsDBRead(Client, DBNumber, Start, Size, Buffer);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_AsDBWrite(IntPtr Client, int DBNumber, int Start, int Size, byte[] Buffer);
        public int AsDBWrite(int DBNumber, int Start, int Size, byte[] Buffer) => Cli_AsDBWrite(Client, DBNumber, Start, Size, Buffer);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_AsMBRead(IntPtr Client, int Start, int Size, byte[] Buffer);
        public int AsMBRead(int Start, int Size, byte[] Buffer) => Cli_AsMBRead(Client, Start, Size, Buffer);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_AsMBWrite(IntPtr Client, int Start, int Size, byte[] Buffer);
        public int AsMBWrite(int Start, int Size, byte[] Buffer) => Cli_AsMBWrite(Client, Start, Size, Buffer);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_AsEBRead(IntPtr Client, int Start, int Size, byte[] Buffer);
        public int AsEBRead(int Start, int Size, byte[] Buffer) => Cli_AsEBRead(Client, Start, Size, Buffer);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_AsEBWrite(IntPtr Client, int Start, int Size, byte[] Buffer);
        public int AsEBWrite(int Start, int Size, byte[] Buffer) => Cli_AsEBWrite(Client, Start, Size, Buffer);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_AsABRead(IntPtr Client, int Start, int Size, byte[] Buffer);
        public int AsABRead(int Start, int Size, byte[] Buffer) => Cli_AsABRead(Client, Start, Size, Buffer);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_AsABWrite(IntPtr Client, int Start, int Size, byte[] Buffer);
        public int AsABWrite(int Start, int Size, byte[] Buffer) => Cli_AsABWrite(Client, Start, Size, Buffer);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_AsTMRead(IntPtr Client, int Start, int Amount, ushort[] Buffer);
        public int AsTMRead(int Start, int Amount, ushort[] Buffer) => Cli_AsTMRead(Client, Start, Amount, Buffer);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_AsTMWrite(IntPtr Client, int Start, int Amount, ushort[] Buffer);
        public int AsTMWrite(int Start, int Amount, ushort[] Buffer) => Cli_AsTMWrite(Client, Start, Amount, Buffer);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_AsCTRead(IntPtr Client, int Start, int Amount, ushort[] Buffer);
        public int AsCTRead(int Start, int Amount, ushort[] Buffer) => Cli_AsCTRead(Client, Start, Amount, Buffer);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_AsCTWrite(IntPtr Client, int Start, int Amount, ushort[] Buffer);
        public int AsCTWrite(int Start, int Amount, ushort[] Buffer) => Cli_AsCTWrite(Client, Start, Amount, Buffer);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_AsListBlocksOfType(IntPtr Client, int BlockType, ushort[] List);
        public int AsListBlocksOfType(int BlockType, ushort[] List) => Cli_AsListBlocksOfType(Client, BlockType, List);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_AsReadSZL(IntPtr Client, int ID, int Index, ref S7SZL Data, ref int Size);
        public int AsReadSZL(int ID, int Index, ref S7SZL Data, ref int Size) => Cli_AsReadSZL(Client, ID, Index, ref Data, ref Size);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_AsReadSZLList(IntPtr Client, ref S7SZLList List, ref int ItemsCount);
        public int AsReadSZLList(ref S7SZLList List, ref int ItemsCount) => Cli_AsReadSZLList(Client, ref List, ref ItemsCount);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_AsUpload(IntPtr Client, int BlockType, int BlockNum, byte[] UsrData, ref int Size);
        public int AsUpload(int BlockType, int BlockNum, byte[] UsrData, ref int Size) => Cli_AsUpload(Client, BlockType, BlockNum, UsrData, ref Size);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_AsFullUpload(IntPtr Client, int BlockType, int BlockNum, byte[] UsrData, ref int Size);
        public int AsFullUpload(int BlockType, int BlockNum, byte[] UsrData, ref int Size) => Cli_AsFullUpload(Client, BlockType, BlockNum, UsrData, ref Size);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_AsDownload(IntPtr Client, int BlockNum, byte[] UsrData, int Size);
        public int ASDownload(int BlockNum, byte[] UsrData, int Size) => Cli_AsDownload(Client, BlockNum, UsrData, Size);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_AsPlcCopyRamToRom(IntPtr Client, uint Timeout);
        public int AsPlcCopyRamToRom(uint Timeout) => Cli_AsPlcCopyRamToRom(Client, Timeout);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_AsPlcCompress(IntPtr Client, uint Timeout);
        public int AsPlcCompress(uint Timeout) => Cli_AsPlcCompress(Client, Timeout);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_AsDBGet(IntPtr Client, int DBNumber, byte[] UsrData, ref int Size);
        public int AsDBGet(int DBNumber, byte[] UsrData, ref int Size) => Cli_AsDBGet(Client, DBNumber, UsrData, ref Size);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_AsDBFill(IntPtr Client, int DBNumber, int FillChar);
        public int AsDBFill(int DBNumber, int FillChar) => Cli_AsDBFill(Client, DBNumber, FillChar);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_CheckAsCompletion(IntPtr Client, ref int opResult);
        public bool CheckAsCompletion(ref int opResult) => Cli_CheckAsCompletion(Client, ref opResult) == JobComplete;

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_WaitAsCompletion(IntPtr Client, int Timeout);
        public int WaitAsCompletion(int Timeout) => Cli_WaitAsCompletion(Client, Timeout);

        #endregion

        #region [Info Functions]

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_GetExecTime(IntPtr Client, ref uint Time);
        public int ExecTime()
        {
            var Time = new uint();
            if (Cli_GetExecTime(Client, ref Time) == 0)
                return (int)(Time);
            else
                return -1;
        }

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_GetLastError(IntPtr Client, ref int LastError);
        public int LastError()
        {
            var ClientLastError = new int();
            if (Cli_GetLastError(Client, ref ClientLastError) == 0)
                return ClientLastError;
            else
                return -1;
        }

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_GetPduLength(IntPtr Client, ref int Requested, ref int Negotiated);

        public int RequestedPduLength()
        {
            var Requested = new int();
            var Negotiated = new int();
            if (Cli_GetPduLength(Client, ref Requested, ref Negotiated) == 0)
                return Requested;
            else
                return -1;
        }

        public int NegotiatedPduLength()
        {
            var Requested = new int();
            var Negotiated = new int();
            if (Cli_GetPduLength(Client, ref Requested, ref Negotiated) == 0)
                return Negotiated;
            else
                return -1;
        }

        [DllImport(S7Consts.Snap7LibName, CharSet = CharSet.Ansi)]
        protected static extern int Cli_ErrorText(int Error, StringBuilder ErrMsg, int TextSize);
        public string ErrorText(int Error)
        {
            var Message = new StringBuilder(MsgTextLen);
            Cli_ErrorText(Error, Message, MsgTextLen);
            return Message.ToString();
        }

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Cli_GetConnected(IntPtr Client, ref uint IsConnected);
        public bool Connected()
        {
            var IsConnected = new uint();
            if (Cli_GetConnected(Client, ref IsConnected) == 0)
                return IsConnected != 0;
            else
                return false;
        }

        #endregion
    }

    public class S7Server
    {
        #region [Constants, private vars and TypeDefs]

        private const int MsgTextLen = 1024;
        private const int mkEvent = 0;
        private const int mkLog = 1;

        // S7 Area ID
        public const byte S7AreaPE = 0x81;
        public const byte S7AreaPA = 0x82;
        public const byte S7AreaMK = 0x83;
        public const byte S7AreaDB = 0x84;
        public const byte S7AreaCT = 0x1C;
        public const byte S7AreaTM = 0x1D;
        // S7 Word Length
        public const int S7WLBit = 0x01;
        public const int S7WLByte = 0x02;
        public const int S7WLWord = 0x04;
        public const int S7WLDWord = 0x06;
        public const int S7WLReal = 0x08;
        public const int S7WLCounter = 0x1C;
        public const int S7WLTimer = 0x1D;

        // Server Area ID  (use with Register/unregister - Lock/unlock Area)
        public static readonly int srvAreaPE = 0;
        public static readonly int srvAreaPA = 1;
        public static readonly int srvAreaMK = 2;
        public static readonly int srvAreaCT = 3;
        public static readonly int srvAreaTM = 4;
        public static readonly int srvAreaDB = 5;
        // Errors
        public static readonly uint errSrvCannotStart = 0x00100000; // Server cannot start
        public static readonly uint errSrvDBNullPointer = 0x00200000; // Passed null as PData
        public static readonly uint errSrvAreaAlreadyExists = 0x00300000; // Area Re-registration
        public static readonly uint errSrvUnknownArea = 0x00400000; // Unknown area
        public static readonly uint errSrvInvalidParams = 0x00500000; // Invalid param(s) supplied
        public static readonly uint errSrvTooManyDB = 0x00600000; // Cannot register DB
        public static readonly uint errSrvInvalidParamNumber = 0x00700000; // Invalid param (srv_get/set_param)
        public static readonly uint errSrvCannotChangeParam = 0x00800000; // Cannot change because running
        // TCP Server Event codes
        public static readonly uint evcServerStarted = 0x00000001;
        public static readonly uint evcServerStopped = 0x00000002;
        public static readonly uint evcListenerCannotStart = 0x00000004;
        public static readonly uint evcClientAdded = 0x00000008;
        public static readonly uint evcClientRejected = 0x00000010;
        public static readonly uint evcClientNoRoom = 0x00000020;
        public static readonly uint evcClientException = 0x00000040;
        public static readonly uint evcClientDisconnected = 0x00000080;
        public static readonly uint evcClientTerminated = 0x00000100;
        public static readonly uint evcClientsDropped = 0x00000200;
        public static readonly uint evcReserved_00000400 = 0x00000400; // actually unused
        public static readonly uint evcReserved_00000800 = 0x00000800; // actually unused
        public static readonly uint evcReserved_00001000 = 0x00001000; // actually unused
        public static readonly uint evcReserved_00002000 = 0x00002000; // actually unused
        public static readonly uint evcReserved_00004000 = 0x00004000; // actually unused
        public static readonly uint evcReserved_00008000 = 0x00008000; // actually unused
        // S7 Server Event Code
        public static readonly uint evcPDUincoming = 0x00010000;
        public static readonly uint evcDataRead = 0x00020000;
        public static readonly uint evcDataWrite = 0x00040000;
        public static readonly uint evcNegotiatePDU = 0x00080000;
        public static readonly uint evcReadSZL = 0x00100000;
        public static readonly uint evcClock = 0x00200000;
        public static readonly uint evcUpload = 0x00400000;
        public static readonly uint evcDownload = 0x00800000;
        public static readonly uint evcDirectory = 0x01000000;
        public static readonly uint evcSecurity = 0x02000000;
        public static readonly uint evcControl = 0x04000000;
        public static readonly uint evcReserved_08000000 = 0x08000000; // actually unused
        public static readonly uint evcReserved_10000000 = 0x10000000; // actually unused
        public static readonly uint evcReserved_20000000 = 0x20000000; // actually unused
        public static readonly uint evcReserved_40000000 = 0x40000000; // actually unused
        public static readonly uint evcReserved_80000000 = 0x80000000; // actually unused
        // Masks to enable/disable all events
        public static readonly uint evcAll = 0xFFFFFFFF;
        public static readonly uint evcNone = 0x00000000;
        // Event SubCodes
        public static readonly ushort evsUnknown = 0x0000;
        public static readonly ushort evsStartUpload = 0x0001;
        public static readonly ushort evsStartDownload = 0x0001;
        public static readonly ushort evsGetBlockList = 0x0001;
        public static readonly ushort evsStartListBoT = 0x0002;
        public static readonly ushort evsListBoT = 0x0003;
        public static readonly ushort evsGetBlockInfo = 0x0004;
        public static readonly ushort evsGetClock = 0x0001;
        public static readonly ushort evsSetClock = 0x0002;
        public static readonly ushort evsSetPassword = 0x0001;
        public static readonly ushort evsClrPassword = 0x0002;
        // Event Params : functions group
        public static readonly ushort grProgrammer = 0x0041;
        public static readonly ushort grCyclicData = 0x0042;
        public static readonly ushort grBlocksInfo = 0x0043;
        public static readonly ushort grSZL = 0x0044;
        public static readonly ushort grPassword = 0x0045;
        public static readonly ushort grBSend = 0x0046;
        public static readonly ushort grClock = 0x0047;
        public static readonly ushort grSecurity = 0x0045;
        // Event Params : control codes
        public static readonly ushort CodeControlUnknown = 0x0000;
        public static readonly ushort CodeControlColdStart = 0x0001;
        public static readonly ushort CodeControlWarmStart = 0x0002;
        public static readonly ushort CodeControlStop = 0x0003;
        public static readonly ushort CodeControlCompress = 0x0004;
        public static readonly ushort CodeControlCpyRamRom = 0x0005;
        public static readonly ushort CodeControlInsDel = 0x0006;
        // Event Result
        public static readonly ushort evrNoError = 0x0000;
        public static readonly ushort evrFragmentRejected = 0x0001;
        public static readonly ushort evrMalformedPDU = 0x0002;
        public static readonly ushort evrSparseBytes = 0x0003;
        public static readonly ushort evrCannotHandlePDU = 0x0004;
        public static readonly ushort evrNotImplemented = 0x0005;
        public static readonly ushort evrErrException = 0x0006;
        public static readonly ushort evrErrAreaNotFound = 0x0007;
        public static readonly ushort evrErrOutOfRange = 0x0008;
        public static readonly ushort evrErrOverPDU = 0x0009;
        public static readonly ushort evrErrTransportSize = 0x000A;
        public static readonly ushort evrInvalidGroupUData = 0x000B;
        public static readonly ushort evrInvalidSZL = 0x000C;
        public static readonly ushort evrDataSizeMismatch = 0x000D;
        public static readonly ushort evrCannotUpload = 0x000E;
        public static readonly ushort evrCannotDownload = 0x000F;
        public static readonly ushort evrUploadInvalidID = 0x0010;
        public static readonly ushort evrResNotFound = 0x0011;

        // Read/Write Operation (to be used into RWCallback)
        public static readonly int OperationRead = 0;
        public static readonly int OperationWrite = 1;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct USrvEvent
        {
            public IntPtr EvtTime;   // It's platform dependent (32 or 64 bit)
            public int EvtSender;
            public uint EvtCode;
            public ushort EvtRetCode;
            public ushort EvtParam1;
            public ushort EvtParam2;
            public ushort EvtParam3;
            public ushort EvtParam4;
        }

        public struct SrvEvent
        {
            public DateTime EvtTime;
            public int EvtSender;
            public uint EvtCode;
            public ushort EvtRetCode;
            public ushort EvtParam1;
            public ushort EvtParam2;
            public ushort EvtParam3;
            public ushort EvtParam4;
        }

        private readonly Dictionary<int, GCHandle> HArea;

        private IntPtr Server;

        #endregion

        #region [Class Control]

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern IntPtr Srv_Create();
        public S7Server()
        {
            Server = Srv_Create();
            HArea = new Dictionary<int, GCHandle>();
        }

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Srv_Destroy(ref IntPtr Server);
        ~S7Server()
        {
            foreach (var Item in HArea)
            {
                var handle = Item.Value;
                if (handle != null)
                    handle.Free();
            }
            Srv_Destroy(ref Server);
        }

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Srv_StartTo(IntPtr Server, [MarshalAs(UnmanagedType.LPStr)] string Address);
        public int StartTo(string Address) => Srv_StartTo(Server, Address);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Srv_Start(IntPtr Server);
        public int Start() => Srv_Start(Server);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Srv_Stop(IntPtr Server);
        public int Stop() => Srv_Stop(Server);

        // Get/SetParam needs a void* parameter, internally it decides the kind of pointer
        // in accord to ParamNumber.
        // To avoid the use of unsafe code we split the DLL functions and use overloaded methods.

        [DllImport(S7Consts.Snap7LibName, EntryPoint = "Srv_GetParam")]
        protected static extern int Srv_GetParam_i16(IntPtr Server, int ParamNumber, ref short IntValue);
        public int GetParam(int ParamNumber, ref short IntValue) => Srv_GetParam_i16(Server, ParamNumber, ref IntValue);

        [DllImport(S7Consts.Snap7LibName, EntryPoint = "Srv_GetParam")]
        protected static extern int Srv_GetParam_u16(IntPtr Server, int ParamNumber, ref ushort IntValue);
        public int GetParam(int ParamNumber, ref ushort IntValue) => Srv_GetParam_u16(Server, ParamNumber, ref IntValue);

        [DllImport(S7Consts.Snap7LibName, EntryPoint = "Srv_GetParam")]
        protected static extern int Srv_GetParam_i32(IntPtr Server, int ParamNumber, ref int IntValue);
        public int GetParam(int ParamNumber, ref int IntValue) => Srv_GetParam_i32(Server, ParamNumber, ref IntValue);

        [DllImport(S7Consts.Snap7LibName, EntryPoint = "Srv_GetParam")]
        protected static extern int Srv_GetParam_u32(IntPtr Server, int ParamNumber, ref uint IntValue);
        public int GetParam(int ParamNumber, ref uint IntValue) => Srv_GetParam_u32(Server, ParamNumber, ref IntValue);

        [DllImport(S7Consts.Snap7LibName, EntryPoint = "Srv_GetParam")]
        protected static extern int Srv_GetParam_i64(IntPtr Server, int ParamNumber, ref long IntValue);
        public int GetParam(int ParamNumber, ref long IntValue) => Srv_GetParam_i64(Server, ParamNumber, ref IntValue);

        [DllImport(S7Consts.Snap7LibName, EntryPoint = "Srv_GetParam")]
        protected static extern int Srv_GetParam_u64(IntPtr Server, int ParamNumber, ref ulong IntValue);
        public int GetParam(int ParamNumber, ref ulong IntValue) => Srv_GetParam_u64(Server, ParamNumber, ref IntValue);

        [DllImport(S7Consts.Snap7LibName, EntryPoint = "Srv_SetParam")]
        protected static extern int Srv_SetParam_i16(IntPtr Server, int ParamNumber, ref short IntValue);
        public int SetParam(int ParamNumber, ref short IntValue) => Srv_SetParam_i16(Server, ParamNumber, ref IntValue);

        [DllImport(S7Consts.Snap7LibName, EntryPoint = "Srv_SetParam")]
        protected static extern int Srv_SetParam_u16(IntPtr Server, int ParamNumber, ref ushort IntValue);
        public int SetParam(int ParamNumber, ref ushort IntValue) => Srv_SetParam_u16(Server, ParamNumber, ref IntValue);

        [DllImport(S7Consts.Snap7LibName, EntryPoint = "Srv_SetParam")]
        protected static extern int Srv_SetParam_i32(IntPtr Server, int ParamNumber, ref int IntValue);
        public int SetParam(int ParamNumber, ref int IntValue) => Srv_SetParam_i32(Server, ParamNumber, ref IntValue);

        [DllImport(S7Consts.Snap7LibName, EntryPoint = "Srv_SetParam")]
        protected static extern int Srv_SetParam_u32(IntPtr Server, int ParamNumber, ref uint IntValue);
        public int SetParam(int ParamNumber, ref uint IntValue) => Srv_SetParam_u32(Server, ParamNumber, ref IntValue);

        [DllImport(S7Consts.Snap7LibName, EntryPoint = "Srv_SetParam")]
        protected static extern int Srv_SetParam_i64(IntPtr Server, int ParamNumber, ref long IntValue);
        public int SetParam(int ParamNumber, ref long IntValue) => Srv_SetParam_i64(Server, ParamNumber, ref IntValue);

        [DllImport(S7Consts.Snap7LibName, EntryPoint = "Srv_SetParam")]
        protected static extern int Srv_SetParam_u64(IntPtr Server, int ParamNumber, ref ulong IntValue);
        public int SetParam(int ParamNumber, ref ulong IntValue) => Srv_SetParam_u64(Server, ParamNumber, ref IntValue);

        #endregion

        #region [Data Areas functions]

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Srv_RegisterArea(IntPtr Server, int AreaCode, int Index, IntPtr pUsrData, int Size);
        public int RegisterArea<T>(int AreaCode, int Index, ref T pUsrData, int Size)
        {
            var AreaUID = (AreaCode << 16) + Index;
            var handle = GCHandle.Alloc(pUsrData, GCHandleType.Pinned);
            var Result = Srv_RegisterArea(Server, AreaCode, Index, handle.AddrOfPinnedObject(), Size);
            if (Result == 0)
                HArea.Add(AreaUID, handle);
            else
                handle.Free();
            return Result;
        }

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Srv_UnregisterArea(IntPtr Server, int AreaCode, int Index);
        public int UnregisterArea(int AreaCode, int Index)
        {
            var Result = Srv_UnregisterArea(Server, AreaCode, Index);
            if (Result == 0)
            {
                var AreaUID = (AreaCode << 16) + Index;
                if (HArea.ContainsKey(AreaUID)) // should be always true
                {
                    var handle = HArea[AreaUID];
                    if (handle != null) // should be always true
                        handle.Free();
                    HArea.Remove(AreaUID);
                }
            }
            return Result;
        }

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Srv_LockArea(IntPtr Server, int AreaCode, int Index);
        public int LockArea(int AreaCode, int Index) => Srv_LockArea(Server, AreaCode, Index);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Srv_UnlockArea(IntPtr Server, int AreaCode, int Index);
        public int UnlockArea(int AreaCode, int Index) => Srv_UnlockArea(Server, AreaCode, Index);

        #endregion

        #region [Notification functions (Events)]

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct RWBuffer
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)] // A telegram cannot exceed PDU size (960 bytes)
            public byte[] Data;
        }

        public delegate void TSrvCallback(IntPtr usrPtr, ref USrvEvent Event, int Size);
        public delegate int TSrvRWAreaCallback(IntPtr usrPtr, int Sender, int Operation, ref S7Consts.S7Tag Tag, ref RWBuffer Buffer);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Srv_SetEventsCallback(IntPtr Server, TSrvCallback Callback, IntPtr usrPtr);
        public int SetEventsCallBack(TSrvCallback Callback, IntPtr usrPtr) => Srv_SetEventsCallback(Server, Callback, usrPtr);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Srv_SetReadEventsCallback(IntPtr Server, TSrvCallback Callback, IntPtr usrPtr);
        public int SetReadEventsCallBack(TSrvCallback Callback, IntPtr usrPtr) => Srv_SetReadEventsCallback(Server, Callback, usrPtr);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Srv_SetRWAreaCallback(IntPtr Server, TSrvRWAreaCallback Callback, IntPtr usrPtr);
        public int SetRWAreaCallBack(TSrvRWAreaCallback Callback, IntPtr usrPtr) => Srv_SetRWAreaCallback(Server, Callback, usrPtr);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Srv_PickEvent(IntPtr Server, ref USrvEvent Event, ref int EvtReady);
        public bool PickEvent(ref USrvEvent Event)
        {
            var EvtReady = new int();
            if (Srv_PickEvent(Server, ref Event, ref EvtReady) == 0)
                return EvtReady != 0;
            else
                return false;
        }

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Srv_ClearEvents(IntPtr Server);
        public int ClearEvents() => Srv_ClearEvents(Server);

        [DllImport(S7Consts.Snap7LibName, CharSet = CharSet.Ansi)]
        protected static extern int Srv_EventText(ref USrvEvent Event, StringBuilder EvtMsg, int TextSize);
        public string EventText(ref USrvEvent Event)
        {
            var Message = new StringBuilder(MsgTextLen);
            Srv_EventText(ref Event, Message, MsgTextLen);
            return Message.ToString();
        }

        public DateTime EvtTimeToDateTime(IntPtr TimeStamp)
        {
            var UnixStartEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return UnixStartEpoch.AddSeconds(Convert.ToDouble(TimeStamp));
        }

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Srv_GetMask(IntPtr Server, int MaskKind, ref uint Mask);
        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Srv_SetMask(IntPtr Server, int MaskKind, uint Mask);

        // Property LogMask R/W
        public uint LogMask
        {
            get
            {
                var Mask = new uint();
                if (Srv_GetMask(Server, S7Server.mkLog, ref Mask) == 0)
                    return Mask;
                else
                    return 0;
            }
            set => Srv_SetMask(Server, S7Server.mkLog, value);
        }

        // Property EventMask R/W
        public uint EventMask
        {
            get
            {
                var Mask = new uint();
                if (Srv_GetMask(Server, S7Server.mkEvent, ref Mask) == 0)
                    return Mask;
                else
                    return 0;
            }
            set => Srv_SetMask(Server, S7Server.mkEvent, value);
        }


        #endregion

        #region [Info functions]

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Srv_GetStatus(IntPtr Server, ref int ServerStatus, ref int CpuStatus, ref int ClientsCount);
        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Srv_SetCpuStatus(IntPtr Server, int CpuStatus);

        // Property Virtual CPU status R/W
        public int CpuStatus
        {
            get
            {
                var CStatus = new int();
                var SStatus = new int();
                var CCount = new int();

                if (Srv_GetStatus(Server, ref SStatus, ref CStatus, ref CCount) == 0)
                    return CStatus;
                else
                    return -1;
            }
            set => Srv_SetCpuStatus(Server, value);
        }

        // Property Server Status Read Only
        public int ServerStatus
        {
            get
            {
                var CStatus = new int();
                var SStatus = new int();
                var CCount = new int();
                if (Srv_GetStatus(Server, ref SStatus, ref CStatus, ref CCount) == 0)
                    return SStatus;
                else
                    return -1;
            }
        }

        // Property Clients Count Read Only
        public int ClientsCount
        {
            get
            {
                var CStatus = new int();
                var SStatus = new int();
                var CCount = new int();
                if (Srv_GetStatus(Server, ref CStatus, ref SStatus, ref CCount) == 0)
                    return CCount;
                else
                    return -1;
            }
        }

        [DllImport(S7Consts.Snap7LibName, CharSet = CharSet.Ansi)]
        protected static extern int Srv_ErrorText(int Error, StringBuilder ErrMsg, int TextSize);
        public string ErrorText(int Error)
        {
            var Message = new StringBuilder(MsgTextLen);
            Srv_ErrorText(Error, Message, MsgTextLen);
            return Message.ToString();
        }

        #endregion
    }

    public class S7Partner
    {
        #region [Constants, private vars and TypeDefs]

        private const int MsgTextLen = 1024;

        // Status
        public static readonly int par_stopped = 0;   // stopped
        public static readonly int par_connecting = 1;   // running and active connecting
        public static readonly int par_waiting = 2;   // running and waiting for a connection
        public static readonly int par_linked = 3;   // running and connected : linked
        public static readonly int par_sending = 4;   // sending data
        public static readonly int par_receiving = 5;   // receiving data
        public static readonly int par_binderror = 6;   // error starting passive server

        // Errors
        public static readonly uint errParAddressInUse = 0x00200000;
        public static readonly uint errParNoRoom = 0x00300000;
        public static readonly uint errServerNoRoom = 0x00400000;
        public static readonly uint errParInvalidParams = 0x00500000;
        public static readonly uint errParNotLinked = 0x00600000;
        public static readonly uint errParBusy = 0x00700000;
        public static readonly uint errParFrameTimeout = 0x00800000;
        public static readonly uint errParInvalidPDU = 0x00900000;
        public static readonly uint errParSendTimeout = 0x00A00000;
        public static readonly uint errParRecvTimeout = 0x00B00000;
        public static readonly uint errParSendRefused = 0x00C00000;
        public static readonly uint errParNegotiatingPDU = 0x00D00000;
        public static readonly uint errParSendingBlock = 0x00E00000;
        public static readonly uint errParRecvingBlock = 0x00F00000;
        public static readonly uint errBindError = 0x01000000;
        public static readonly uint errParDestroying = 0x01100000;
        public static readonly uint errParInvalidParamNumber = 0x01200000;
        public static readonly uint errParCannotChangeParam = 0x01300000;

        // Generic byte buffer structure, you may need to declare a more
        // specialistic one in your program.
        // It's used to cast the input pointer that cames from the callback.
        // See the passive partned demo and the delegate S7ParRecvCallback.

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct S7Buffer
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x8000)]
            public byte[] Data;
        }

        // Job status
        private const int JobComplete = 0;
        private const int JobPending = 1;

        private IntPtr Partner;

        private int parBytesSent;
        private int parBytesRecv;
        private int parSendErrors;
        private int parRecvErrors;

        #endregion

        #region [Class Control]

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern IntPtr Par_Create(int ParActive);
        public S7Partner(int Active) => Partner = Par_Create(Active);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Par_Destroy(ref IntPtr Partner);
        ~S7Partner()
        {
            Par_Destroy(ref Partner);
        }

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Par_StartTo(
            IntPtr Partner,
            [MarshalAs(UnmanagedType.LPStr)] string LocalAddress,
            [MarshalAs(UnmanagedType.LPStr)] string RemoteAddress,
            ushort LocalTSAP,
            ushort RemoteTSAP);

        public int StartTo(
            string LocalAddress,
            string RemoteAddress,
            ushort LocalTSAP,
            ushort RemoteTSAP) => Par_StartTo(Partner, LocalAddress, RemoteAddress, LocalTSAP, RemoteTSAP);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Par_Start(IntPtr Partner);
        public int Start() => Par_Start(Partner);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Par_Stop(IntPtr Partner);
        public int Stop() => Par_Stop(Partner);

        // Get/SetParam needs a void* parameter, internally it decides the kind of pointer
        // in accord to ParamNumber.
        // To avoid the use of unsafe code we split the DLL functions and use overloaded methods.

        [DllImport(S7Consts.Snap7LibName, EntryPoint = "Par_GetParam")]
        protected static extern int Par_GetParam_i16(IntPtr Partner, int ParamNumber, ref short IntValue);
        public int GetParam(int ParamNumber, ref short IntValue) => Par_GetParam_i16(Partner, ParamNumber, ref IntValue);

        [DllImport(S7Consts.Snap7LibName, EntryPoint = "Par_GetParam")]
        protected static extern int Par_GetParam_u16(IntPtr Partner, int ParamNumber, ref ushort IntValue);
        public int GetParam(int ParamNumber, ref ushort IntValue) => Par_GetParam_u16(Partner, ParamNumber, ref IntValue);

        [DllImport(S7Consts.Snap7LibName, EntryPoint = "Par_GetParam")]
        protected static extern int Par_GetParam_i32(IntPtr Partner, int ParamNumber, ref int IntValue);
        public int GetParam(int ParamNumber, ref int IntValue) => Par_GetParam_i32(Partner, ParamNumber, ref IntValue);

        [DllImport(S7Consts.Snap7LibName, EntryPoint = "Par_GetParam")]
        protected static extern int Par_GetParam_u32(IntPtr Partner, int ParamNumber, ref uint IntValue);
        public int GetParam(int ParamNumber, ref uint IntValue) => Par_GetParam_u32(Partner, ParamNumber, ref IntValue);

        [DllImport(S7Consts.Snap7LibName, EntryPoint = "Par_GetParam")]
        protected static extern int Par_GetParam_i64(IntPtr Partner, int ParamNumber, ref long IntValue);
        public int GetParam(int ParamNumber, ref long IntValue) => Par_GetParam_i64(Partner, ParamNumber, ref IntValue);

        [DllImport(S7Consts.Snap7LibName, EntryPoint = "Par_GetParam")]
        protected static extern int Par_GetParam_u64(IntPtr Partner, int ParamNumber, ref ulong IntValue);
        public int GetParam(int ParamNumber, ref ulong IntValue) => Par_GetParam_u64(Partner, ParamNumber, ref IntValue);

        [DllImport(S7Consts.Snap7LibName, EntryPoint = "Par_SetParam")]
        protected static extern int Par_SetParam_i16(IntPtr Partner, int ParamNumber, ref short IntValue);
        public int SetParam(int ParamNumber, ref short IntValue) => Par_SetParam_i16(Partner, ParamNumber, ref IntValue);

        [DllImport(S7Consts.Snap7LibName, EntryPoint = "Par_SetParam")]
        protected static extern int Par_SetParam_u16(IntPtr Partner, int ParamNumber, ref ushort IntValue);
        public int SetParam(int ParamNumber, ref ushort IntValue) => Par_SetParam_u16(Partner, ParamNumber, ref IntValue);

        [DllImport(S7Consts.Snap7LibName, EntryPoint = "Par_SetParam")]
        protected static extern int Par_SetParam_i32(IntPtr Partner, int ParamNumber, ref int IntValue);
        public int SetParam(int ParamNumber, ref int IntValue) => Par_SetParam_i32(Partner, ParamNumber, ref IntValue);

        [DllImport(S7Consts.Snap7LibName, EntryPoint = "Par_SetParam")]
        protected static extern int Par_SetParam_u32(IntPtr Partner, int ParamNumber, ref uint IntValue);
        public int SetParam(int ParamNumber, ref uint IntValue) => Par_SetParam_u32(Partner, ParamNumber, ref IntValue);

        [DllImport(S7Consts.Snap7LibName, EntryPoint = "Par_SetParam")]
        protected static extern int Par_SetParam_i64(IntPtr Partner, int ParamNumber, ref long IntValue);
        public int SetParam(int ParamNumber, ref long IntValue) => Par_SetParam_i64(Partner, ParamNumber, ref IntValue);

        [DllImport(S7Consts.Snap7LibName, EntryPoint = "Par_SetParam")]
        protected static extern int Par_SetParam_u64(IntPtr Partner, int ParamNumber, ref ulong IntValue);
        public int SetParam(int ParamNumber, ref ulong IntValue) => Par_SetParam_u64(Partner, ParamNumber, ref IntValue);

        #endregion

        #region [Data I/O functions : BSend]

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Par_BSend(IntPtr Partner, uint R_ID, byte[] Buffer, int Size);
        public int BSend(uint R_ID, byte[] Buffer, int Size) => Par_BSend(Partner, R_ID, Buffer, Size);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Par_AsBSend(IntPtr Partner, uint R_ID, byte[] Buffer, int Size);
        public int AsBSend(uint R_ID, byte[] Buffer, int Size) => Par_AsBSend(Partner, R_ID, Buffer, Size);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Par_CheckAsBSendCompletion(IntPtr Partner, ref int opResult);
        public bool CheckAsBSendCompletion(ref int opResult) => Par_CheckAsBSendCompletion(Partner, ref opResult) == JobComplete;

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Par_WaitAsBSendCompletion(IntPtr Partner, int Timeout);
        public int WaitAsBSendCompletion(int Timeout) => Par_WaitAsBSendCompletion(Partner, Timeout);

        public delegate void S7ParSendCompletion(IntPtr usrPtr, int opResult);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Par_SetSendCallback(IntPtr Partner, S7ParSendCompletion Completion, IntPtr usrPtr);
        public int SetSendCallBack(S7ParSendCompletion Completion, IntPtr usrPtr) => Par_SetSendCallback(Partner, Completion, usrPtr);

        #endregion

        #region [Data I/O functions : BRecv]

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Par_BRecv(IntPtr Partner, ref uint R_ID, byte[] Buffer, ref int Size, uint Timeout);
        public int BRecv(ref uint R_ID, byte[] Buffer, ref int Size, uint Timeout) => Par_BRecv(Partner, ref R_ID, Buffer, ref Size, Timeout);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Par_CheckAsBRecvCompletion(IntPtr Partner, ref int opResult, ref uint R_ID, byte[] Buffer, ref int Size);
        public bool CheckAsBRecvCompletion(ref int opResult, ref uint R_ID, byte[] Buffer, ref int Size)
        {
            Par_CheckAsBRecvCompletion(Partner, ref opResult, ref R_ID, Buffer, ref Size);
            return opResult == JobComplete;
        }

        public delegate void S7ParRecvCallback(IntPtr usrPtr, int opResult, uint R_ID, IntPtr pData, int Size);

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Par_SetRecvCallback(IntPtr Partner, S7ParRecvCallback Callback, IntPtr usrPtr);
        public int SetRecvCallback(S7ParRecvCallback Callback, IntPtr usrPtr) => Par_SetRecvCallback(Partner, Callback, usrPtr);

        #endregion

        #region [Info functions]

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Par_GetLastError(IntPtr Partner, ref int LastError);
        public int LastError(ref int LastError)
        {
            var PartnerLastError = new int();
            if (Par_GetLastError(Partner, ref PartnerLastError) == 0)
                return PartnerLastError;
            else
                return -1;
        }

        [DllImport(S7Consts.Snap7LibName, CharSet = CharSet.Ansi)]
        protected static extern int Par_ErrorText(int Error, StringBuilder ErrMsg, int TextSize);
        public string ErrorText(int Error)
        {
            var Message = new StringBuilder(MsgTextLen);
            Par_ErrorText(Error, Message, MsgTextLen);
            return Message.ToString();
        }

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Par_GetStats(IntPtr Partner, ref int BytesSent, ref int BytesRecv,
           ref int SendErrors, ref int RecvErrors);

        private void GetStatistics()
        {
            if (Par_GetStats(Partner, ref parBytesSent, ref parBytesRecv, ref parSendErrors, ref parRecvErrors) != 0)
            {
                parBytesSent = -1;
                parBytesRecv = -1;
                parSendErrors = -1;
                parRecvErrors = -1;
            }
        }

        public int BytesSent
        {
            get
            {
                GetStatistics();
                return parBytesSent;
            }
        }

        public int BytesRecv
        {
            get
            {
                GetStatistics();
                return parBytesRecv;
            }
        }

        public int SendErrors
        {
            get
            {
                GetStatistics();
                return parSendErrors;
            }
        }

        public int RecvErrors
        {
            get
            {
                GetStatistics();
                return parRecvErrors;
            }
        }

        [DllImport(S7Consts.Snap7LibName)]
        protected static extern int Par_GetStatus(IntPtr Partner, ref int Status);

        public int Status
        {
            get
            {
                var ParStatus = new int();
                if (Par_GetStatus(Partner, ref ParStatus) != 0)
                    return -1;
                else
                    return ParStatus;
            }
        }
        // simply useful
        public bool Linked => Status == par_linked;
        #endregion

    }
}
