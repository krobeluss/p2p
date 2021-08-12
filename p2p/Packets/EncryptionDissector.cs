using System;
using System.Collections.Generic;
using System.Text;

namespace P2P.Packets
{
    internal abstract class EncryptionDissector : IPacketDissector
    {
        public abstract byte[] Assembly(IPacketData packet);
        public abstract IPacketData Dissect(byte[] data);


    }
}
