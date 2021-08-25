using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace P2P.Packets.Structures.Common
{
    class Ping : CommonLayer
    {
        public override byte Header => CommonHeaderConstants.PING;

        public ulong Value { get => value; set => this.value = value; }

        private UInt64 value;


        public Ping()
        {

        }

        public Ping(byte[] data)
        {
            BinaryReader br = new BinaryReader(new MemoryStream(data));

            value = br.ReadUInt64();
        }

        public override byte[] Assembly()
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            bw.Write(value);

            return ms.ToArray();
        }
    }
}
