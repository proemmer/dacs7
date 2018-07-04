using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace Dacs7.Protocols.SiemensPlc.Datagrams
{
    internal class S7UserDataDatagram
    {

        public S7HeaderDatagram Header { get; set; } = new S7HeaderDatagram
        {
            PduType = 0x07, //UserData - > Should be a marker
            DataLength = 0,
            ParamLength = 14 // default fo r1 item
        };

        public S7UserDataParameter Parameter { get; set; }

        public S7UserData Data { get; set; }




        public static Memory<byte> TranslateToMemory(S7UserDataDatagram datagram)
        {
            var offset = datagram.Header.GetHeaderSize() + datagram.Parameter.GetParamSize();
            var dataSize = offset + datagram.Data.GetUserDataLength();
            var result = S7HeaderDatagram.TranslateToMemory(datagram.Header, dataSize);
            S7UserDataParameter.TranslateToMemory(datagram.Parameter, result.Slice(datagram.Header.GetHeaderSize()));
            result.Span[offset++] = datagram.Data.ReturnCode;
            result.Span[offset++] = datagram.Data.TransportSize;
            BinaryPrimitives.WriteUInt16BigEndian(result.Slice(offset, 2).Span, datagram.Data.UserDataLength);
            datagram.Data.Data.CopyTo(result.Slice(offset+2, datagram.Data.UserDataLength));
            return result;
        }

        public static S7UserDataDatagram TranslateFromMemory(Memory<byte> data)
        {
            var span = data.Span;
            var result = new S7UserDataDatagram
            {
                Header = S7HeaderDatagram.TranslateFromMemory(data),
            };

            result.Parameter = S7UserDataParameter.TranslateFromMemory(data.Slice(result.Header.GetHeaderSize()));
            var offset = result.Header.GetHeaderSize() + result.Parameter.GetParamSize();
            result.Data.ReturnCode = span[offset++];
            result.Data.TransportSize = span[offset++];
            result.Data.UserDataLength = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset, 2));
            return result;
        }
    }
}
