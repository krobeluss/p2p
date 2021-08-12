using System;
using System.Collections.Generic;
using System.Text;

namespace P2P.Packets
{
    interface IPayloadablePacketData : IPacketData
    {
        byte[] Payload { get; set; }
    }
}
