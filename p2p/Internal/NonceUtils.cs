using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace P2P.Internal
{
    internal class NonceUtils
    {
        private bool isEven;
        private int nonceSize;
        private UInt64 sequenceNum;
        private int timestampOffset;
        private int nonceLifeTimme;

        HashSet<byte[]> usedNonces = new HashSet<byte[]>();

        public NonceUtils(bool even, int nonceSize, int nonceLifeTimme, int timestampOffset)
        {
            this.isEven = even;
            this.nonceSize = nonceSize;
            this.timestampOffset = timestampOffset;
            this.nonceLifeTimme = nonceLifeTimme;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public byte[] GetNextNonce()
        {
            byte[] nonce = new byte[nonceSize];
            MemoryStream ms = new MemoryStream(nonce);
            BinaryWriter bw = new BinaryWriter(ms);

            bw.Write((byte)(isEven ? 1 : 0));

            bw.Write(++sequenceNum);
            bw.Write(DateTimeOffset.Now.ToUnixTimeMilliseconds() + timestampOffset);

            return nonce;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool TrackNonce(byte[] nonce)
        {
            if (nonce.Length != nonceSize)
                throw new ArgumentException("Illegal nonce array size");

            if (usedNonces.Contains(nonce))
                throw new Exception("Nonce used");

            MemoryStream ms = new MemoryStream(nonce);
            BinaryReader br = new BinaryReader(ms);

            ms.Seek(9, SeekOrigin.Current);
            Int64 genarationTimestamp = br.ReadInt64();

            if (genarationTimestamp + nonceLifeTimme < DateTimeOffset.Now.ToUnixTimeMilliseconds() + timestampOffset)
                throw new Exception("Nonce expired");

            usedNonces.Add(nonce);
            return true;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void RemoveExpiredNonces()
        {
            usedNonces.RemoveWhere((nonce) =>
            {
                MemoryStream ms = new MemoryStream(nonce);
                BinaryReader br = new BinaryReader(ms);

                ms.Seek(9, SeekOrigin.Current);
                Int64 genarationTimestamp = br.ReadInt64();

                if (genarationTimestamp + nonceLifeTimme < DateTimeOffset.Now.ToUnixTimeMilliseconds() + timestampOffset)
                    return true;

                return false;
            });
        }
    }
}
