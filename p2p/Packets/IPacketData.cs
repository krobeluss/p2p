using System;
using System.Collections.Generic;
using System.Text;

namespace P2P.Packets
{
    internal interface IPacketData
    {
        byte[] Assembly();
    }
}
