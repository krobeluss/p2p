using NaCl;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace P2P.Packets.Structures.Encryption
{
    internal class NaClDissector : EncryptionDissector
    {

        private Curve25519XSalsa20Poly1305 cryptor;

        public NaClDissector(Curve25519XSalsa20Poly1305 cryptor)
        {
            this.cryptor = cryptor;
        }

        public override byte[] Assembly(IPacketData packet)
        {
            if(typeof(NaClPacket).IsInstanceOfType(packet) )
            {
                NaClPacket naclPacket = (NaClPacket)packet;
                byte[] data = new byte[Curve25519XSalsa20Poly1305.NonceLength + Curve25519XSalsa20Poly1305.TagLength + naclPacket.Payload.Length];

                Array.Copy(naclPacket.Nonce, data, Curve25519XSalsa20Poly1305.NonceLength);

                lock(cryptor)
                    cryptor.Encrypt(data, Curve25519XSalsa20Poly1305.NonceLength, naclPacket.Payload, 0, naclPacket.Payload.Length, naclPacket.Nonce, 0);

                return data;
            }

            throw new ArgumentException();
        }

        public override IPacketData Dissect(byte[] data)
        {
            byte[] message = new byte[data.Length - Curve25519XSalsa20Poly1305.NonceLength - Curve25519XSalsa20Poly1305.TagLength];
            byte[] nonce = new byte[Curve25519XSalsa20Poly1305.NonceLength];

            Array.Copy(data, nonce, Curve25519XSalsa20Poly1305.NonceLength);

            //byte[] message, int messageOffset, byte[] cipher, int cipherOffset, int cipherCount, byte[] nonce, int nonceOffset

            bool success;

            lock (cryptor)
                success = cryptor.TryDecrypt(message, 0, data, Curve25519XSalsa20Poly1305.NonceLength, data.Length - Curve25519XSalsa20Poly1305.NonceLength, nonce, 0);


            if (success)
            {
                NaClPacket structure = new NaClPacket();
                structure.Nonce = nonce;
                structure.Payload = message;

                return structure;
            }
            

            throw new CryptographicException();
        }
    }
}
