using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace P2P
{
    public class VirtualUdpSocket
    {
        internal BlockingCollection<KeyValuePair<VirtualEndpoint, byte[]>> incomingPackets;
       
        private int bindPort;

        internal PrivateNetwork network;

        public int BindPort { get => bindPort; }

        public byte[] Receive(out VirtualEndpoint from)
        {
            KeyValuePair<VirtualEndpoint, byte[]> gotPacket = incomingPackets.Take();

            from = gotPacket.Key;

            return gotPacket.Value;
        }

        public void Send(byte[] data, VirtualEndpoint to)
        {
            network.SendVirtualUdpPacket(bindPort, to, data);
        }
    }
}
