using System;
using System.Collections.Generic;
using System.Text;

namespace P2P.Packets
{
    interface IPacketData
    {
        byte[] Assembly();
    }
}
