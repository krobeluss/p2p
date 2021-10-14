using P2P.Packets.Structures.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace P2P.Internal
{
    internal interface IVirtualTcpSocket
    {
        internal void ProcessFrame(TcpData data, VirtualEndpoint from);


    }
}
