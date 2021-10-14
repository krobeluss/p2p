using System;
using System.Collections.Generic;
using System.Text;

namespace P2P
{
    public class VirtualEndpoint
    {
        private UInt32 id;
        private int port;

        internal static VirtualEndpoint ANY = new VirtualEndpoint(0, 0);

        public VirtualEndpoint(uint id, int port)
        {
            this.id = id;
            this.port = port;
        }

        public uint Id { get => id; }
        public int Port { get => port;}

        public override bool Equals(object obj)
        {
            return obj is VirtualEndpoint endpoint &&
                   id == endpoint.id &&
                   port == endpoint.port;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(id, port);
        }
    }
}
