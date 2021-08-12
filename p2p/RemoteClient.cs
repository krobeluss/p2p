using P2P.Packets;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace P2P
{
    internal class RemoteClient
    {
        internal IPEndPoint externalAddress;
        internal IPEndPoint internalAddress;

        internal IPEndPoint correctAddress;
        private PrivateNetwork network;

        internal UInt32 remoteID;
        internal int helloCount = 0;


        public bool IsConnected
        {
            get
            {
                return correctAddress != null;
            }
        }

        public RemoteClient(IPEndPoint externalAddress, IPEndPoint internalAddress, UInt32 remoteID)
        {
            this.externalAddress = externalAddress;
            this.internalAddress = internalAddress;
            this.remoteID = remoteID;
        }

        public void ProcessPacket (IPayloadablePacketData packet)
        {

        }
    }
}
