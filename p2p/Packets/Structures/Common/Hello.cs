using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace P2P.Packets.Structures.P2P
{
    class Hello : CommonLayer
    {

        private const int COMPRESSION_ENABLED = 1;
        private const int LIVE_TCP = 1 << 1;

        public override byte Header => CommonHeaderConstants.HELLO;

        public bool CompressionEnabled
        {
            get
            {
                return ( flags & COMPRESSION_ENABLED ) == COMPRESSION_ENABLED;
            }
            set
            {
                if (value)
                    flags |= COMPRESSION_ENABLED;
                else
                    flags &= ~COMPRESSION_ENABLED;
            }
        }

        public bool LiveTcp
        {
            get
            {
                return (flags & LIVE_TCP) == LIVE_TCP;
            }

            set
            {
                if (value)
                    flags |= LIVE_TCP;
                else
                    flags &= ~LIVE_TCP;
            }
        }

        private int flags;

        public Hello(byte[] data)
        {
            BinaryReader br = new BinaryReader(new MemoryStream(data));

            flags = br.ReadInt32();
        }

        public Hello()
        {

        }

        public override byte[] Assembly()
        {
            byte[] flags = BitConverter.GetBytes(this.flags);
            Array.Reverse(flags);

            return flags;
        }
    }
}
