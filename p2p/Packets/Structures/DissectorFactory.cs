using NaCl;
using P2P.Packets.Structures.Encryption;
using System;
using System.Collections.Generic;
using System.Text;

namespace P2P.Packets.Structures
{

    internal enum AddressDissectorType
    {
        ID
    }

    internal class DissectorFactory
    {

        public IPacketDissector GetAddressDissector(AddressDissectorType type)
        {
            if (type == AddressDissectorType.ID)
            {
                return new AddressDissector();
            }

            throw new ArgumentException("Unknown type");
        }

        public IPacketDissector GetEncryptionDissector(Curve25519XSalsa20Poly1305 cryptor)
        {
            return new NaClDissector(cryptor);
        }

        public IPacketDissector GetCommonDissector()
        {
            return new CommonDissector();
        }
    }
}
