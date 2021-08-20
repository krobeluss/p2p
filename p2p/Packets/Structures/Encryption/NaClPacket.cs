using NaCl;
using System;
using System.Collections.Generic;
using System.Text;

namespace P2P.Packets.Structures.Encryption
{
    internal class NaClPacket : IPayloadablePacketData
    {
        private byte[] payload;

        private byte[] nonce;

        public byte[] Payload { get => payload; set => payload = value; }
        public byte[] Nonce { get => nonce; set {
                if (value.Length == Curve25519XSalsa20Poly1305.NonceLength)
                    nonce = value;
                else
                    throw new ArgumentException();
            }
        }

        public byte[] Assembly()
        {
            return payload;
        }
    }
}
