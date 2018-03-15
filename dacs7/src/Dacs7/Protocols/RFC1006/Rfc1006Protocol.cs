using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dacs7.Protocols.RFC1006
{
    internal class Rfc1006ProtocolHandler : IUpperProtocolHandler
    {
        #region Const

        private const byte TpduDr = 0x80;
        private const byte TpduEr = 0x70;

        private const byte TpdtDtEotFlag = 0x80;

        private const byte TpduSizeMin = 0x04;
        private const byte TpduSizeMax = 0x0e;
        public const int TpduSizeStandard = 1024; // 1024 = 0x0a (2^10)

        private const byte ParmCodeTpduSize = 0xc0;
        private const byte ParmCodeSrcTsap = 0xc1;
        private const byte ParmCodeDestTsap = 0xc2;

        private const int TpduDrOffsetInTpkt = Tpkt.HeaderLength + TpduDt.HeaderLength;

        #endregion const

        #region fields

        private byte[] _tpktBuffer = { };
        private byte[] _packetBuffer = { };

        private readonly Tsap _tsap;
        private byte SizeTpduReceiving { get; set; }
        private byte SizeTpduSending { get; set; }

        #endregion

        public Func<byte[], Task<SocketError>> SocketSendFunc { get; set; }
        public bool Connected { get; private set; }

        public Rfc1006ProtocolHandler(Func<byte[], Task<SocketError>> send,string tsapLocal, string tsapRemote, int frameSize = TpduSizeStandard)
        {
            SocketSendFunc = send;
            _tsap = new Tsap(tsapRemote, tsapLocal);
            var b = -1;
            for (var i = frameSize; i > 0; i = i >> 1, ++b) ;
            b = Math.Max(TpduSizeMin, Math.Min(TpduSizeMax, b));
            SizeTpduSending = SizeTpduReceiving = (byte)b;
        }


        public void Connect()
        {
            Send(Tpkt.WrapHeader(TpduCr.WrapHeader(CreateCotpParams())));
        }

        public void Shutdown()
        {
            Connected = false;
        }

        public byte[] AddUpperProtocolFrame(byte[] txData)
        {
            var frameSize = 1 << SizeTpduSending;
            var b = new byte[] { };
            do
            {
                // Take a frame
                var frame = txData.Take(frameSize).ToArray();
                txData = txData.Skip(frameSize).ToArray();

                b = b.Concat(Tpkt.WrapHeader((new byte[]
                {
                    0x02,   // Header Length -> sizeof DT -1
                    TpduDt.PduType,
                    (byte) (txData.Length > 0 ? 0x00 : TpdtDtEotFlag)
                }).Concat(frame).ToArray())).ToArray();
            } while (txData.Length > 0);
            return b;
        }

        public IEnumerable<byte[]> RemoveUpperProtocolFrame(byte[] rxData, int count)
        {
            var dataPackets = new List<byte[]>();
            _tpktBuffer = _tpktBuffer.Concat(rxData.Take(count)).ToArray();

            while (true)
            {
                var length = Tpkt.Length(_tpktBuffer);
                if (length <= 0)
                {
                    Thread.Sleep(1);
                    return dataPackets;
                }

                if (_tpktBuffer.Length < length)
                {
                    return dataPackets;
                }

                var packet = HandleTpkt(_tpktBuffer.Take(length).ToArray());
                if (packet == null)
                    Thread.Sleep(1);
                dataPackets.Add(packet);

                _tpktBuffer = _tpktBuffer.Skip(length).ToArray();
                if (_tpktBuffer.Length <= 0)
                    return dataPackets;
            }
        }

        private byte[] HandleTpkt(byte[] tpkt)
        {
            if (TpduCr.IsType(tpkt))
            {
                return TpduConnect(tpkt);
            }
            if (TpduCc.IsType(tpkt))
            {
                return TpduConnect(tpkt, true);
            }
            if (TpduDt.IsType(tpkt))
            {
                if (Connected)
                {
                    // packetBuffer collects all DT_Packaged up to EOT!!!
                    _packetBuffer = _packetBuffer.Concat(tpkt.Skip(TpduDrOffsetInTpkt).ToArray()).ToArray();
                    if (tpkt[4] == 0x02 && tpkt[6] == TpdtDtEotFlag)
                    {
                        var retBuffer = _packetBuffer;
                        _packetBuffer = new byte[] { };
                        return retBuffer;
                    }
                    return null;
                }
                return null;
            }

            _tpktBuffer = new byte[] { };
            return null;
        }

        private byte[] TpduConnect(byte[] rxData, bool weAreClient = false)
        {

            var tsap1 = GetTsap(rxData, ParmCodeSrcTsap);
            var tsap2 = GetTsap(rxData, ParmCodeDestTsap);
            var tsapSrc = weAreClient ? tsap2 : tsap1;
            var tsapDst = weAreClient ? tsap1 : tsap2;
            if (String.Compare(_tsap.Local, tsapDst, StringComparison.Ordinal) == 0 &&
                String.Compare(_tsap.Remote, tsapSrc, StringComparison.Ordinal) == 0)
            {

                if (weAreClient)
                {
                    Connected = true;
                    return null;
                }

                rxData[5] = TpduCc.PduType;
                for (var i = 11; i < rxData.Length; i++)
                {
                    if (rxData[i] == ParmCodeTpduSize)
                    {
                        SizeTpduSending = rxData[i + 2];
                        rxData[i + 2] = SizeTpduReceiving;
                        break;
                    }
                }

                Connected = Send(rxData) == SocketError.Success;
            }
            else
            {
                Send(new byte[] { });
            }
            return null;
        }

        private byte[] CreateCotpParams()
        {
            var sendData = new byte[]
            {
                ParmCodeTpduSize,   // code that identifies TPDU size
                0x01,                  // 1 byte this field
                SizeTpduReceiving
            };

            sendData = sendData.Concat(new byte[]
            {
                ParmCodeSrcTsap,           // code that identifies source TSAP
                (byte) _tsap.Local.Length     // source TSAP Len
            }).ToArray();
            sendData = sendData.Concat(Encoding.ASCII.GetBytes(_tsap.Local).ToArray()).ToArray();


            sendData = sendData.Concat(new byte[]
            {
                ParmCodeDestTsap,          // code that identifies destination TSAP
                (byte) _tsap.Remote.Length    // destination TSAP Len
            }).ToArray();
            sendData = sendData.Concat(Encoding.ASCII.GetBytes(_tsap.Remote).ToArray()).ToArray();

            return sendData;
        }

        private static string GetTsap(byte[] data, byte paramCode)
        {
            for (var i = 11; i < data.Length; i++)
            {
                if (data[i] == paramCode)
                {
                    return Encoding.ASCII.GetString(data, i + 2, data[i + 1]);
                }
            }
            return string.Empty;
        }

        private SocketError Send(byte[] data)
        {
            var send = SocketError.SocketError;
            if (SocketSendFunc != null)
                send = SocketSendFunc(data).Result;
            return send;
        }

        #region Static Helper Classes

        /// <summary>
        /// TPDUS shall contain, in the following order:
        /// a.) the header, comprising:
        ///     1) the Length Indicator (LI) field;
        ///     2) the fixed part;
        ///     3) the variable part, if present;
        /// b.) the data field, if present
        /// 
        /// Octets:  1      2 3 4 ... n      n + 1 ... p      p + 1  ... end 
        ///        -------------------------------------------------------------
        ///        | LI |   Fixed part   |  Variable part  |    Data field     |
        ///        -------------------------------------------------------------
        /// </summary>

        /// <summary>
        /// Connection Request (CR)
        /// </summary>
        private static class TpduCr
        {
            //Header
            //BYTE li;
            //BYTE cc_cdt;
            //USHORT dst_ref;
            //USHORT src_ref;
            //BYTE class_option;
            public const byte PduType = 0xe0;  // TDPU Type CR = Connection Request (see RFC1006/ISO8073)
            public const int HeaderLength = 7;

            public static byte[] WrapHeader(byte[] data)
            {
                data = (new byte[] {
                    (byte) (data.Length + 6), //0x1d,
                    PduType,
                    0x00, 0x00,  // TPDU Destination Reference (unknown)
                    0x00, 0x01,  // TPDU Source-Reference (my own reference, should not be zero)
                    0x00         // TPDU Class 0 and no Option
                }).Concat(data.ToArray()).ToArray();
                return data;
            }

            public static bool IsType(byte[] data)
            {
                if (data == null || data.Length < Tpkt.HeaderLength + HeaderLength)
                {
                    return false;
                }
                return data.AsSpan().Slice(Tpkt.HeaderLength + 1)[0].CompareTo(PduType) == 0;
            }
        }


        /// <summary>
        /// Connection Confirm (CC)
        /// </summary>
        private static class TpduCc
        {
            //Header
            //BYTE li;
            //BYTE cc_cdt;
            //USHORT dst_ref;
            //USHORT src_ref;
            //BYTE class_option;
            public const byte PduType = 0xd0;
            public const int HeaderLength = 7;

            public static bool IsType(byte[] data)
            {
                if (data == null || data.Length < Tpkt.HeaderLength + HeaderLength)
                    return false;

                return data.AsSpan().Slice(Tpkt.HeaderLength + 1)[0].CompareTo(PduType) == 0;
            }
        }



        /// <summary>
        /// Data Transfer (DT)
        /// </summary>
        private static class TpduDt
        {
            //Header
            //BYTE li;
            //BYTE dt_roa;
            //BYTE tpdu_nr;
            public const byte PduType = 0xf0;
            public const int HeaderLength = 3;

            public static bool IsType(byte[] data)
            {
                if (data == null || data.Length < Tpkt.HeaderLength + HeaderLength)
                {
                    return false;
                }
                return data.AsSpan().Slice(Tpkt.HeaderLength + 1)[0].CompareTo(PduType) == 0;
            }
        }


        private static class Tpkt
        {
            //Header
            //  BYTE sync1;
            //  BYTE sync2;
            //  USHORT len;
            public const byte SYNC_1 = 0x03;
            public const byte SYNC_2 = 0x00;
            public const int HeaderLength = 4;

            public static byte[] WrapHeader(byte[] data)
            {
                data = (new byte[]
                {
                    SYNC_1,
                    SYNC_2,
                    0xff, 0xff
                }).Concat(data.ToArray()).ToArray();
                BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan().Slice(2), (ushort)(data.Length));
                return data;
            }

            public static ushort Length(byte[] data)
            {
                if (data == null || data.Length < HeaderLength || data[0] != SYNC_1 || data[1] != SYNC_2)
                {
                    return 0;
                }
                
                return BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan().Slice(2));
            }
        }
        #endregion

    }
}
