using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace P2P.Packets.Structures.Address
{
    class IDAdderss : AderssLayer
    {
        protected byte[] payload;
        private UInt32 fromID;
        public override byte[] Payload { get => payload; set => payload = value; }
       
        public uint FromID { get => fromID; set => fromID = value; }

        public IDAdderss()
        {

        }

        public IDAdderss(byte[] data)
        {
            MemoryStream ms = new MemoryStream(data);
            BinaryReader br = new BinaryReader(ms);

            fromID = br.ReadUInt32();

            payload = new byte[data.Length - 4];
            ms.Read(payload, 0, payload.Length);
        }

        public override byte[] Assembly()
        {
            MemoryStream ms = new MemoryStream(payload.Length + 4);
            BinaryWriter bw = new BinaryWriter(ms);

            bw.Write(fromID);
            bw.Write(payload);

            return ms.ToArray();
        }
    }
}
