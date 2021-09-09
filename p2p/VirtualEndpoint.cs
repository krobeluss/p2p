using System;
using System.Collections.Generic;
using System.Text;

namespace P2P
{
    public class VirtualEndpoint
    {
        private UInt32 id;
        private int port;

        public VirtualEndpoint(uint id, int port)
        {
            this.id = id;
            this.port = port;
        }

        public uint Id { get => id; set => id = value; }
        public int Port { get => port; set => port = value; }
    }
}
