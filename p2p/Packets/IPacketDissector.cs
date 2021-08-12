using System;
using System.Collections.Generic;
using System.Text;

namespace P2P.Packets
{
    internal interface IPacketDissector
    { 
        IPacketData Dissect(byte[] data);

        byte[] Assembly(IPacketData packet);
    }
}
