using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace P2P.Packets.Structures.Common
{
    class TcpData : CommonLayer, IPayloadablePacketData
    {

        private const int SYN = 1;
        private const int ACK = 1 << 1;
        private const int FIN = 1 << 2;
        private const int RST = 1 << 3;

        private UInt16 fromPort;

        private UInt16 toPort;

        private UInt32 seq;

        private UInt32 ack;

        private UInt32 win;

        private int flags;

        private byte[] payload;

        public byte[] Payload { get => payload; set => payload = value; }
        public override byte Header => CommonHeaderConstants.TCP_DATA;
        public ushort FromPort { get => fromPort; set => fromPort = value; }
        public ushort ToPort { get => toPort; set => toPort = value; }
        public uint Seq { get => seq; set => seq = value; }
        public uint Ack { get => ack; set => ack = value; }
        public uint Win { get => win; set => win = value; }

        public bool SynFlag
        {
            get
            {
                return (flags & SYN) == SYN;
            }
            set
            {
                if (value)
                    flags |= SYN;
                else
                    flags &= ~SYN;
            }
        }

        public bool AckFlag
        {
            get
            {
                return (flags & ACK) == ACK;
            }

            set
            {
                if (value)
                    flags |= ACK;
                else
                    flags &= ~ACK;
            }
        }

        public bool FinFlag
        {
            get
            {
                return (flags & FIN) == FIN;
            }

            set
            {
                if (value)
                    flags |= FIN;
                else
                    flags &= ~FIN;
            }
        }

        public bool RstFlag
        {
            get
            {
                return (flags & RST) == RST;
            }

            set
            {
                if (value)
                    flags |= RST;
                else
                    flags &= ~RST;
            }
        }

        public TcpData(byte[] data)
        {
            BinaryReader br = new BinaryReader(new MemoryStream(data));

            MemoryStream payloadStream = new MemoryStream();

            fromPort = br.ReadUInt16();
            toPort = br.ReadUInt16();
            seq = br.ReadUInt32();
            ack = br.ReadUInt32();
            win = br.ReadUInt32();
            flags = br.ReadByte();
            br.BaseStream.CopyTo(payloadStream);

            payload = payloadStream.ToArray();
        }

        public TcpData()
        {

        }

        public override byte[] Assembly()
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            bw.Write(fromPort);
            bw.Write(toPort);
            bw.Write(seq);
            bw.Write(ack);
            bw.Write(win);
            bw.Write((byte)flags);
            bw.Write(payload);

            return ms.ToArray();
        }
    }
}
