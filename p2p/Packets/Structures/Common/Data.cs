using System;
using System.Collections.Generic;
using System.Text;

namespace P2P.Packets.Structures.Common
{
    class Data : CommonLayer, IPayloadablePacketData
    {

        private byte[] payload;

        public override byte Header => throw new NotImplementedException();

        public byte[] Payload { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public Data(byte[] data)
        {
            payload = (byte[])data.Clone();
        }

        public override byte[] Assembly()
        {
            return (byte[])payload.Clone();
        }
    }
}
