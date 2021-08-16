using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace P2P.Internal
{
    internal class STUNParser
    {
        public static byte[] CreateStunRequest(byte[] transactionID)
        {
            if (transactionID.Length != 12)
                throw new ArgumentException("transactionID shouldn't be null");

            if (transactionID.Length != 12)
                throw new ArgumentException("transactionID length not 12");

            BinaryWriter bw = new BinaryWriter(new MemoryStream(20));

            // 21 12 a4 42 01 02 03 04 05 06 07 08 09 10 11 12

            bw.Write(new byte[] { 00, 01 }); // Request
            bw.Write((short)0); // Length
            bw.Write(new byte[] { 0x21, 0x12, 0xa4, 0x42 }); // Magic cookie
            bw.Write(transactionID);

            return ((MemoryStream)bw.BaseStream).ToArray();
        }

        public static IPEndPoint ParseSTUNResponse(byte[] data)
        {
            if (data.Length < 20)
                return null;

            BinaryReader br = new BinaryReader(new MemoryStream(data));

            UInt16 messageType = BinaryPrimitives.ReadUInt16BigEndian(br.ReadBytes(2));
            UInt16 length = BinaryPrimitives.ReadUInt16BigEndian(br.ReadBytes(2));
            byte[] cookieAndTransactionID = br.ReadBytes(16);

            if (messageType == 0x0101) // Response
            {
                while(br.BaseStream.Position - 20 < length)
                {
                    // Reading attributes

                    UInt16 attributeType = BinaryPrimitives.ReadUInt16BigEndian(br.ReadBytes(2));
                    UInt16 attributeLength = BinaryPrimitives.ReadUInt16BigEndian(br.ReadBytes(2));

                    if (attributeType != 0x0020)
                        br.BaseStream.Seek(attributeLength, SeekOrigin.Current);
                    else 
                    {
                        br.ReadByte(); // Reserved
                        byte family = br.ReadByte();

                        if (family != 0x01)
                            throw new Exception("Got not IPv4");

                        byte[] xorPort = br.ReadBytes(2);
                        byte[] xorIP = br.ReadBytes(4);

                        xorPort[0] = (byte)(xorPort[0] ^ cookieAndTransactionID[0]);
                        xorPort[1] = (byte)(xorPort[1] ^ cookieAndTransactionID[1]);

                        xorIP[0] = (byte)(xorIP[0] ^ cookieAndTransactionID[0]);
                        xorIP[1] = (byte)(xorIP[1] ^ cookieAndTransactionID[1]);
                        xorIP[2] = (byte)(xorIP[2] ^ cookieAndTransactionID[2]);
                        xorIP[3] = (byte)(xorIP[3] ^ cookieAndTransactionID[3]);

                        return new IPEndPoint(new IPAddress(xorIP), BinaryPrimitives.ReadUInt16BigEndian(xorPort));

                    }

                }
            }

            return null;
        }
    }
}
