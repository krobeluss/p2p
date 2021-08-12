using P2P.Packets.Structures;
using P2P.Packets.Structures.Address;
using System;
using System.Collections.Generic;
using System.Text;

namespace P2P.Packets
{
    internal class AddressDissector : IPacketDissector
    {
        public byte[] Assembly(IPacketData packet)
        {
            if (typeof(AderssLayer).IsInstanceOfType(packet))
                return packet.Assembly();

            throw new ArgumentException();
        }

        public IPacketData Dissect(byte[] data)
        {
            return new IDAdderss(data);
        }
    }
}
