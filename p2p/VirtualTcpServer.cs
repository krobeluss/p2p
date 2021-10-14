using P2P.Internal;
using P2P.Packets.Structures.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace P2P
{
    public class VirtualTcpServer : IVirtualTcpSocket
    {
        internal UInt16 localPort;

        private SocketStatus status;

        private PrivateNetwork network;

        internal VirtualTcpServer(UInt16 localPort, PrivateNetwork network)
        {
            status = SocketStatus.LISTEN;
            this.localPort = localPort;
            this.network = network;
        }

        void IVirtualTcpSocket.ProcessFrame(TcpData data, VirtualEndpoint from)
        {
            if(data.SynFlag)
            {
                VirtualTcpClient newClient = network.OpenTcpClient(from, localPort, true);

                if (newClient != null)
                {
                    newClient.AckCounter = data.Seq + 1;
                    newClient.status = SocketStatus.SYN_RECEIVED;
                    newClient.SendSyn();
                }
            }
        }
    }
}
