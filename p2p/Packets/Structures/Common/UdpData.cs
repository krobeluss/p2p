using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace P2P.Packets.Structures.Common
{
    class UdpData : CommonLayer, IPayloadablePacketData
    {
        public override byte Header => CommonHeaderConstants.UDP_DATA;

        public int FromPort { get => fromPort; set => fromPort = value; }
        public int ToPort { get => toPort; set => toPort = value; }
        public byte[] Payload { get => payload; set => payload = value; }

        private int fromPort;
        private int toPort;
        private byte[] payload;

        public UdpData()
        {

        }

        public UdpData(byte[] data)
        {
            BinaryReader br = new BinaryReader(new MemoryStream(data));

            MemoryStream payloadStream = new MemoryStream();

            fromPort = br.ReadUInt16();
            toPort = br.ReadUInt16();
            br.BaseStream.CopyTo(payloadStream);

            payload = payloadStream.ToArray();
        }

        public override byte[] Assembly()
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            bw.Write((UInt16)fromPort);
            bw.Write((UInt16)toPort);
            bw.Write(payload);

            return ms.ToArray();
        }
    }
}
