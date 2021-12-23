using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace P2P.Internal
{
    public class IPPacket
    {
        private IPAddress sourceAddress;
        private IPAddress destinstionAddress;

        private byte protocol;

        private byte[] payload;

        public IPPacket()
        {

        }

        public IPPacket( byte[] data )
        {
            if(data.Length < 20)
                throw new Exception("O_o");

            MemoryStream ms = new MemoryStream(data);

            byte[] word = new byte[4];
            ms.Read(word, 0, 4);

            if(word[0] != 0x45)
            {
                throw new Exception("Походу прилетел не IP4 пакет))");
            }

            // 2nd

            ms.Read(word, 0, 4);

            // 3nd

            ms.Read(word, 0, 4);
            protocol = word[1];
            ushort checksumm = (ushort)(word[2] << 8 | word[3]);

            ms.Read(word, 0, 4); // Source
            sourceAddress = new IPAddress(word);

            ms.Read(word, 0, 4); // Dest
            destinstionAddress = new IPAddress(word);

            payload = new byte[data.Length - 20];
            ms.Read(payload, 0, payload.Length); // Dest

            // Чексумму бы проверить
        }

        public byte[] Assembly( )
        {
            MemoryStream ms = new MemoryStream(20 + payload.Length);

            int csum = 0;

            byte[] word = new byte[4];
            word[0] = 0x45;
            word[2] = (byte)(((20 + payload.Length) & 0xFF00) >> 8);
            word[3] = (byte)((20 + payload.Length) & 0xFF);

            csum += getPart(word[0], word[1]);
            csum += getPart(word[2], word[3]);

            ms.Write(word, 0, 4);

            // 2nd part

            word[0] = 0;
            word[1] = 0;
            word[2] = 0x40;
            word[3] = 0;

            csum += getPart(word[0], word[1]);
            csum += getPart(word[2], word[3]);

            ms.Write(word, 0, 4); 

            // 3nd part

            word[0] = 128;
            word[1] = protocol;
            word[2] = 0;
            word[3] = 0;

            csum += getPart(word[0], word[1]);
            csum += getPart(word[2], word[3]);

            ms.Write(word, 0, 4);

            // 4nd part

            word = sourceAddress.GetAddressBytes();

            csum += getPart(word[0], word[1]);
            csum += getPart(word[2], word[3]);

            ms.Write(word, 0, 4);

            // 5nd part

            word = destinstionAddress.GetAddressBytes();

            csum += getPart(word[0], word[1]);
            csum += getPart(word[2], word[3]);

            ms.Write(word, 0, 4);

            csum += (int)( ( csum & 0xFFFF0000 ) >> 16 );
            csum = 0xFFFF - (ushort)csum;
            byte[] csumBytes = new byte[2];

            csumBytes[0] = (byte)( (csum & 0xFF00) >> 8);
            csumBytes[1] = (byte)(csum & 0xFF);

            ms.Write(payload);

            byte[] response = ms.ToArray();

            Array.Copy(csumBytes, 0, response, 10, 2);

            return response;
        }

        private static int getPart(byte a, byte b)
        {
            return a << 8 | b; 
        }

        public IPAddress SourceAddress { get => sourceAddress; set => sourceAddress = value; }
        public IPAddress DestinstionAddress { get => destinstionAddress; set => destinstionAddress = value; }
        public byte Protocol { get => protocol; set => protocol = value; }
        public byte[] Payload { get => payload; set => payload = value; }
    }
}
