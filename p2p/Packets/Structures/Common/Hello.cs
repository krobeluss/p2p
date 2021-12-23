using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace P2P.Packets.Structures.P2P
{
    internal class Hello : CommonLayer
    {

        private const int COMPRESSION_ENABLED = 1;
        private const int REMOTE_HELLO_RECIVED =  1 << 2;
        private const int ANSWER_HELLO = 1 << 3;

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

        public bool RemoteHelloReceived
        {
            get
            {
                return (flags & REMOTE_HELLO_RECIVED) == REMOTE_HELLO_RECIVED;
            }

            set
            {
                if (value)
                    flags |= REMOTE_HELLO_RECIVED;
                else
                    flags &= ~REMOTE_HELLO_RECIVED;
            }
        }

        public bool AnswerHello
        {
            get
            {
                return (flags & ANSWER_HELLO) == ANSWER_HELLO;
            }

            set
            {
                if (value)
                    flags |= ANSWER_HELLO;
                else
                    flags &= ~ANSWER_HELLO;
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
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            bw.Write(flags);

            return ms.ToArray();
        }
    }
}
