using System;
using System.Collections.Generic;
using System.Text;

namespace P2P.Packets.Structures
{
    abstract class CommonLayer : IPacketData
    {
        abstract public byte Header { get; }

        abstract public byte[] Assembly();

    }
}
