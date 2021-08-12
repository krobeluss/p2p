using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace P2P.Packets.Structures
{
    internal abstract class AderssLayer : IPayloadablePacketData
    {
        public abstract byte[] Payload { get; set; }

        public abstract byte[] Assembly();
    }
}
